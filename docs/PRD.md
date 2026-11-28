# PRD: Painel de Demandas (MVP em WPF)

13 de novembro de 2026 · Gabriel Sanga

> Transcrito do PDF original em 14 de novembro de 2026. O diagrama de arquitetura, que era uma imagem, foi refeito em texto. Desvios decididos depois deste documento estão registrados nas changes do OpenSpec (`openspec/changes/`).

## Resumo e status

O Painel de Demandas é um app desktop em WPF (.NET) que organiza sessões do Claude Code e do Codex por **demanda**, mostra o status de cada sessão e o progresso do OpenSpec, e abre ou retoma as sessões em abas do Windows Terminal. Os agentes rodam num container Docker isolado.

**Status atual:** painel construído; os agentes rodam só no container.

**Princípio:** o painel complementa os apps oficiais, não compete com eles. Ele só organiza e lança; quem roda os agentes é sempre o terminal. O isolamento é feito pelo **container Docker**: sem rota de rede própria, com saída só pelos destinos liberados no proxy, sem o perfil e as credenciais do Windows. Instalação: [instalacao-container.md](instalacao-container.md).

## Contexto e problema

O trabalho é num ERP em .NET dividido em módulos, cada um num repositório TFS diferente. A rotina exige abrir vários terminais ao mesmo tempo, cada um com uma implementação via OpenSpec ou uma correção, usando Claude Code e Codex.

Dores atuais:

- **Organização:** muitos terminais soltos, sem saber qual pertence a qual demanda. Uma demanda que toca dois módulos vira pedaços separados.
- **Status:** difícil ver quais sessões estão executando, esperando input ou paradas.
- **Retomada:** para voltar a uma sessão é preciso procurar no `/resume` ou usar `claude --continue`.
- **Segurança:** o usuário principal do Windows tem acesso a servidores importantes da empresa. Os agentes devem rodar isolados, alcançando só o TFS e as APIs necessárias.

## Objetivos, não-objetivos e critérios de sucesso

### Objetivos do MVP

1. Agrupar sessões por demanda, com uma demanda podendo envolver vários repositórios.
2. Mostrar o status de cada sessão (ativa, executando, aguardando você, precisa de atenção, encerrada, inativa) para Claude Code e Codex.
3. Mostrar o progresso da change do OpenSpec de cada demanda (tarefas concluídas / total).
4. Abrir uma nova sessão ou retomar uma existente com um clique, numa aba do Windows Terminal já no repositório certo, rodando no container.
5. Avisar com notificação do Windows quando uma sessão precisar de atenção.

### Fora do escopo do MVP

- Terminal embutido dentro do painel.
- Banco de dados, serviço em segundo plano ou API.
- Custo e tokens por sessão, histórico, relatórios.
- Acesso web ou pelo celular.
- Integração com tickets do TFS.
- Reproduzir recursos dos apps oficiais (diff visual, preview, worktrees, PR).

### Critérios de sucesso

- Retomar qualquer sessão em menos de 10 segundos, sem usar `/resume`.
- Ver de relance o que precisa de mim, sem trocar de aba.
- Nenhuma ação do painel roda no Windows a partir de dados escritos pelos agentes.
- Uso diário real após duas semanas; se eu voltar aos terminais soltos, o MVP falhou.

## Decisões tomadas e alternativas avaliadas

| Tema | Decisão | Motivo |
|---|---|---|
| Unidade principal | Demanda (pode ter vários repositórios) | É como o trabalho acontece; nenhuma ferramenta pronta organiza assim |
| Interface | App desktop WPF (.NET 8) | Stack do usuário; Windows; MVVM com CommunityToolkit.Mvvm |
| Papel do painel | Painel + lançador; terminal externo (Windows Terminal) | Embutir terminal (ConPTY + xterm.js) consumiria a maior parte do esforço |
| Agentes | Claude Code e Codex | Os dois têm hooks com payload parecido (`session_id`, `cwd`, `hook_event_name`) |
| Status | Hooks gravam arquivos; o painel lê | Confiável e simples; não depende de interpretar a tela do terminal |
| Persistência | Arquivos JSON (um por sessão + `demandas.json`) | Sem banco, sem serviço, sem concorrência entre sessões |
| Execução | Agentes rodam num container Docker, com rede interna e proxy | O container corta o acesso aos servidores da empresa e às credenciais do Windows |
| Arquitetura | Core separado da UI | Permite trocar WPF por Avalonia ou web no futuro |

### Alternativas avaliadas e por que não bastam sozinhas

| Alternativa | O que resolve | O que falta |
|---|---|---|
| Agent View (`claude agents`) | Status e retomada nativos das sessões do Claude | Só Claude; sem demanda nem OpenSpec. Usar durante o período de teste |
| App Claude Desktop | Sessões paralelas, notificações, diff visual; respeita CLAUDE.md, hooks e settings | Só Claude; agrupa por pasta |
| App do Codex (Windows) | Threads por projeto para o Codex | Só Codex; outra janela, outra lista |
| ccmanager, Claude Squad, Agent Deck | Vários agentes com status | Sem demanda nem OpenSpec; risco de cadeia npm (ccmanager); tmux (Squad, Agent Deck) |

## Ambiente

Tudo roda num só computador Windows.

| Item | Valor |
|---|---|
| Painel | Roda no Windows, com o usuário principal, que tem acesso aos servidores da empresa |
| Agentes | Container `datapilot-agente` (Docker Desktop com WSL2), com o Claude Code e o Codex |
| Rede | Rede interna; saída só pelos destinos liberados no proxy |
| Repositórios | `C:\Fontes` (`/workspace`) e `C:\openspec\ERP` (`/openspec/ERP`) |
| Pasta de status | `C:\ProgramData\Painel\sessoes` (`/status`): o container escreve, o painel só lê |
| Scripts dos agentes | `/opt/painel/abrir.sh` e `/opt/painel/hook.mjs`, na imagem, sem escrita para o agente |
| Dados do painel | `%APPDATA%\Painel\demandas.json`: o container não o enxerga |

## Arquitetura

O painel só lê o que os agentes escrevem; os agentes rodam isolados.

```
  WINDOWS (painel)             PASTAS MONTADAS                 CONTAINER

  +------------------+                                         +------------------+
  | demandas.json    |         +------------------+    grava   | hook.mjs         |
  | demandas, repos, |         | Pasta de status  |<-----------| grava status,    |
  | sessoes          |         | container escreve|            | sem saida        |
  +------------------+         | painel le        |            +------------------+
        ^                      +------------------+                     ^
        | le e grava                    |                               | eventos
        v                               v                               |
  +------------------+         +------------------+    edita   +------------------+
  | Painel WPF       |<--------| C:\Fontes e      |<-----------| Claude Code /    |
  | status, OpenSpec,|   le    | C:\openspec\ERP  |            | Codex            |
  | botoes           |         |                  |            |                  |
  +------------------+         +------------------+            +------------------+
        |                                                               ^
        | Abrir                                                         |
        v                                                               |
  +------------------+              docker exec                +------------------+
  | Windows Terminal |---------------------------------------->| abrir.sh         |
  | uma aba por      |                                         | valida e inicia  |
  | sessao           |                                         | o agente         |
  +------------------+                                         +------------------+
```

O painel lê a pasta de status e o `tasks.md`, mas monta comandos só a partir do `demandas.json`. O único caminho do painel até os agentes é a aba do Windows Terminal com `docker exec`, que chama o `abrir.sh`.

## Modelo de dados e contratos

Cinco contratos ligam as partes. Os comandos marcados "a confirmar" devem ser checados com `claude --help` e `codex --help` na versão instalada.

### 1. demandas.json (painel)

Fonte única para montar comandos. Só o painel escreve.

```json
{
  "demandas": [
    {
      "id": "CHG-142",
      "titulo": "Ajuste NF-e",
      "openspecChange": "ajuste-nfe-v4",
      "criadaEm": "2026-11-13T10:00:00-03:00",
      "arquivada": false,
      "repositorios": [
        {
          "nome": "Faturamento",
          "pasta": "Faturamento",
          "sessoes": [
            { "agente": "claude", "execucao": "container", "sessionId": "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10", "rotulo": "implementação", "criadaEm": "2026-11-13T10:05:00-03:00" }
          ]
        }
      ]
    }
  ]
}
```

`pasta` é o nome de uma pasta direta da raiz dos fontes (`[A-Za-z0-9_.-]`) ou o caminho completo de uma pasta dos locais montados.

### 2. Arquivo de status (escrito pelo container)

Um arquivo por sessão em `C:\ProgramData\Painel\sessoes\<session_id>.json`. O painel só exibe esses dados; nunca os usa para montar comandos.

```json
{"agente":"claude","session_id":"0b6f1c2e-...","cwd":"/workspace/Faturamento","status":"aguardando","evento":"Stop","atualizado":"2026-11-13T10:22:31-03:00"}
```

| Evento do hook | Status | Claude | Codex |
|---|---|---|---|
| `SessionStart` | ativa | sim | sim |
| `UserPromptSubmit` | executando | sim | sim |
| `Stop` | aguardando você | sim | sim |
| `Notification` | precisa de atenção | sim | a confirmar |
| `SessionEnd` | encerrada | sim | a confirmar |
| sem atualização há mais de 15 min (status ativa ou executando) | inativa (calculado pelo painel) | — | — |

### 3. hook.mjs (roda no container)

Regras obrigatórias: nunca escrever na saída padrão (em `SessionStart` e `UserPromptSubmit` a saída vira contexto do modelo), sempre terminar com código 0 (no Claude, código 2 bloqueia a ação) e ser rápido. Antes do primeiro status, cria o marcador `<session_id>.criado`, que dá a data de início da sessão. A configuração dos hooks está em `docker/painel`; o Codex pede para revisar e confiar neles pelo comando `/hooks`.

### 4. abrir.sh (roda no container, chamado por docker exec)

Recebe só parâmetros validados, sem shell intermediário: `--agente`, `--repo` ou `--caminho` (caminho no container em Base64), `--sessao` e `--modo` (`novo` ou `retomar`).

No Claude, o painel gera o GUID da sessão antes de abrir. No Codex, o ID só aparece no hook `SessionStart`: o painel registra um lançamento pendente e vincula a sessão quando ela surge na pasta de status.

### 5. OpenSpec

O progresso vem de `<raiz>\<pasta>\openspec\changes\<openspecChange>\tasks.md`: conta linhas que casam com `^\s*-\s\[( |x|X)\]`, separando marcadas e não marcadas. Changes arquivadas ficam em `openspec\changes\archive\` (a confirmar na versão do OpenSpec usada).

## Interface

O painel é uma janela WPF que organiza e lança; os agentes rodam em abas de uma janela própria do Windows Terminal.

### Janela do painel

- **Cabeçalho:** contagem do que precisa de você ("1 precisa de atenção · 2 executando").
- **Coluna esquerda:** lista de demandas com o resumo (repositórios, sessões, pior status); botão "Nova demanda"; filtro de arquivadas.
- **Área direita (demanda selecionada):** título, nome da change do OpenSpec e barra de progresso ("7 de 12 tarefas"); um card por repositório com as sessões (agente, rótulo, última atualização, chip de status, botão "Abrir"); botões "Nova sessão" e "Vincular repositório"; sessões não vinculadas aparecem com botão "Vincular".
- **Cores de status:** vermelho = precisa de atenção; azul = executando; amarelo = aguardando você; cinza = encerrada ou inativa.
- **Bandeja do sistema:** ícone com o número de sessões que precisam de atenção; notificação do Windows quando uma sessão entra em "atenção" ou "aguardando".

### Botão "Abrir" / "Nova sessão"

```
wt -w painel nt --title "CHG-142 · Faturamento" --suppressApplicationTitle docker exec --interactive --tty datapilot-agente /opt/painel/abrir.sh --agente claude --repo Faturamento --sessao <guid> --modo retomar
```

- `-w painel` junta todas as abas dos agentes numa janela separada dos terminais do dia a dia.
- O painel monta a linha apenas com valores do `demandas.json`, passados como argumentos separados (`ProcessStartInfo.ArgumentList`), nunca concatenando texto vindo de arquivos de status.
- **Limitação:** o Windows Terminal não permite focar uma aba já aberta. Se a sessão estiver ativa, o painel avisa "já aberta no terminal" e pede confirmação antes de abrir outra aba.

**Stack sugerida:** .NET 8, WPF, CommunityToolkit.Mvvm, System.Text.Json, `FileSystemWatcher` com releitura periódica a cada 5 s como garantia, um pacote de notificações/ícone de bandeja para WPF (ex.: Hardcodet.NotifyIcon.Wpf).

## Requisitos funcionais por etapa

Cada etapa entrega algo usável no dia a dia; só avance quando a anterior estiver em uso.

| Etapa | Entrega | Critério de aceite |
|---|---|---|
| 0. Pré-requisito | Ambiente em container | O container sobe; servidores importantes inacessíveis a partir dele |
| 1. Scripts | Pasta de status; `hook.mjs`; hooks configurados no Claude e no Codex; `abrir.sh` | Abrir e usar uma sessão de cada agente gera e atualiza o JSON de status; o agente não mostra nada estranho no contexto; `abrir.sh` funciona chamado à mão via `docker exec` |
| 2. Tela de sessões | Solution com `Painel.Core` + `Painel.Wpf`; lista de todas as sessões lidas da pasta de status, com cor por status e regra de "inativa" | Status muda na tela em até 5 s após o evento; arquivos inválidos são ignorados sem travar |
| 3. Abrir e retomar | Botões que montam o comando `wt` + `docker exec` + `abrir.sh` | Nova sessão do Claude abre com GUID gerado pelo painel; retomar abre a conversa certa; aba com título da demanda |
| 4. Demandas | `demandas.json`; criar, editar e arquivar demandas; vincular repositórios e sessões (inclusive sessões não vinculadas do Codex) | Uma demanda com 2 repositórios e sessões dos 2 agentes aparece agrupada |
| 5. OpenSpec e avisos | Barra de progresso do `tasks.md`; ícone na bandeja; notificação em "atenção" e "aguardando" | Progresso bate com o `tasks.md`; notificação aparece com o painel minimizado |

Testes unitários em `Painel.Core.Tests` para: parser do `tasks.md`, mapeamento evento → status, regra de inatividade, validação de GUID e nome de pasta, montagem dos argumentos do `wt`.

## Requisitos de segurança

O painel roda no Windows, com acesso aos servidores da empresa. Nada escrito pelos agentes pode virar uma ação executada no Windows.

1. **RS-1:** arquivos de status são apenas dados para exibir. Nunca montar comandos, caminhos ou argumentos a partir deles.
2. **RS-2:** comandos são montados só com valores do `demandas.json`, passados como argumentos separados; sem shell intermediário.
3. **RS-3:** validar `sessionId` (GUID) e `pasta` (`[A-Za-z0-9_.-]`) no painel e de novo no `abrir.sh`.
4. **RS-4:** o painel nunca escreve na pasta de status; o agente não alcança o `demandas.json` nem altera os scripts do painel na imagem.
5. **RS-5:** ler arquivos de status com limite de tamanho (ex.: 16 KB) e ignorar os inválidos.
6. **RS-6:** hooks sem saída padrão e com `exit 0` sempre; sem segredos nos arquivos de status.
7. **RS-7:** o agente roda no container, sem privilégios nem capabilities, numa rede sem saída direta; só vê `C:\Fontes`, `C:\openspec\ERP`, a pasta de status e os próprios volumes, nunca o socket do Docker nem o perfil do Windows; o proxy libera só a lista de destinos; scripts e hooks do Claude ficam na imagem, sem escrita para o agente; o nome do container é validado (`[A-Za-z0-9][A-Za-z0-9_.-]{0,62}`) antes de entrar no comando.
8. **RS-8:** o agente escreve nas pastas montadas: revisar o diff antes de compilar no Windows, com atenção a `.csproj`, `Directory.Build.props`, `.targets`, `nuget.config`, scripts `.ps1`, `.git/hooks` e `.vscode/tasks.json`.
9. **RS-9:** apresentar o desenho à TI ou à segurança da empresa.

## Riscos, limitações e questões em aberto

| Risco ou limitação | Impacto | Mitigação |
|---|---|---|
| Fechar a aba pode interromper o agente | Trabalho em andamento para | Conversa recuperada com `--resume`; sessões persistentes ficam para depois do MVP |
| `SessionEnd` não dispara ao fechar à força | Status preso em "executando" | Regra de inatividade após 15 min sem atualização |
| Terminal dentro do container: sem colar imagem, sem janelas gráficas, navegador não abre sozinho | Pequenas fricções | `dotnet build/test` no Windows; login copiando o link |
| Hooks do Codex mudam entre versões | Status do Codex pode quebrar | Fixar a versão; testar após atualizar |
| Apps oficiais evoluem e passam a cobrir o caso | Painel perde valor | Manter o escopo pequeno; reavaliar antes de cada etapa |

### Questões em aberto

- [ ] Os hooks do Codex disparam também no app do Codex, ou só na CLI?
- [ ] Tempo de inatividade ideal (15 min é um chute inicial).

## Como continuar numa nova sessão

```
Leia o docs/PRD.md e o docs/arquitetura.md. Eles descrevem o DataPilot, um app WPF (.NET) que organiza
sessões do Claude Code e do Codex por demanda, lê status gravado por hooks e abre as
sessões no Windows Terminal, dentro de um container Docker.

Antes de escrever código:
1. Siga os contratos da seção "Modelo de dados e contratos" sem mudar nomes de campos.
2. Respeite todos os requisitos de segurança (RS-1 a RS-9).
3. Proponha a mudança como uma change do OpenSpec.
```
