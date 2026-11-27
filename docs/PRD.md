# PRD: Painel de Demandas (MVP em WPF)

13 de novembro de 2026 · Gabriel Sanga

> Transcrito do PDF original em 14 de novembro de 2026. O diagrama de arquitetura, que era uma imagem, foi refeito em texto. Desvios decididos depois deste documento estão registrados nas changes do OpenSpec (`openspec/changes/`).

## Resumo e status

O Painel de Demandas é um app desktop em WPF (.NET) que organiza sessões do Claude Code e do Codex por **demanda**, mostra o status de cada sessão e o progresso do OpenSpec, e abre ou retoma as sessões em abas do Windows Terminal. Os agentes rodam num container Docker isolado; o modo Windows (SSH no usuário isolado IA) é a alternativa.

**Status atual:** isolamento pronto (usuário IA sem administrador + servidor SSH local configurado). Nenhum código do painel foi escrito.

**Gatilho para construir:** usar por cerca de duas semanas só as ferramentas prontas dentro do usuário IA (`claude agents` para o Claude e `codex resume` para o Codex). Construir o painel apenas se continuarem faltando: visão por demanda que envolve vários repositórios, progresso do OpenSpec e Claude + Codex numa lista só.

**Princípio:** o painel complementa os apps oficiais, não compete com eles. Ele só organiza e lança; quem roda os agentes é sempre o terminal. O isolamento é feito pelo **container Docker** (padrão): sem rota de rede própria, com saída só pelos destinos liberados no proxy, sem o perfil e as credenciais do Windows. O usuário `ia` (aba aberta por `ssh`) passa a ser a alternativa "Windows" das configurações; nela, desligar o SSH faz o agente rodar com os acessos de quem usa o painel. Instalação: [instalacao-container.md](instalacao-container.md).

> As seções "Ambiente", "Arquitetura" e "Modelo de dados" abaixo descrevem o modo Windows, o desenho original.

## Contexto e problema

O trabalho é num ERP em .NET dividido em módulos, cada um num repositório TFS diferente. A rotina exige abrir vários terminais ao mesmo tempo, cada um com uma implementação via OpenSpec ou uma correção, usando Claude Code e Codex.

Dores atuais:

- **Organização:** muitos terminais soltos, sem saber qual pertence a qual demanda. Uma demanda que toca dois módulos vira pedaços separados.
- **Status:** difícil ver quais sessões estão executando, esperando input ou paradas.
- **Retomada:** para voltar a uma sessão é preciso procurar no `/resume` ou usar `claude --continue`.
- **Segurança:** o usuário principal do Windows tem acesso a servidores importantes da empresa. Os agentes devem rodar num usuário separado que só alcança o TFS e as APIs necessárias.

## Objetivos, não-objetivos e critérios de sucesso

### Objetivos do MVP

1. Agrupar sessões por demanda, com uma demanda podendo envolver vários repositórios.
2. Mostrar o status de cada sessão (ativa, executando, aguardando você, precisa de atenção, encerrada, inativa) para Claude Code e Codex.
3. Mostrar o progresso da change do OpenSpec de cada demanda (tarefas concluídas / total).
4. Abrir uma nova sessão ou retomar uma existente com um clique, numa aba do Windows Terminal já no repositório certo, rodando como o usuário IA.
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
- Nenhuma ação do painel roda como o usuário principal a partir de dados escritos pelo usuário IA.
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
| Execução | Agentes rodam num container Docker, com rede interna e proxy; alternativa: usuário IA por SSH local | O usuário IA não limita a rede; o container corta o acesso aos servidores da empresa e às credenciais do Windows |
| Arquitetura | Core separado da UI | Permite trocar WPF por Avalonia ou web no futuro |

### Alternativas avaliadas e por que não bastam sozinhas

| Alternativa | O que resolve | O que falta |
|---|---|---|
| Agent View (`claude agents`) | Status e retomada nativos das sessões do Claude | Só Claude; sem demanda nem OpenSpec. Usar durante o período de teste |
| App Claude Desktop | Sessões paralelas, notificações, diff visual; respeita CLAUDE.md, hooks e settings | Só Claude; agrupa por pasta; sessões SSH com host Windows ainda instáveis |
| App do Codex (Windows) | Threads por projeto para o Codex | Só Codex; outra janela, outra lista |
| ccmanager, Claude Squad, Agent Deck | Vários agentes com status | Sem demanda nem OpenSpec; risco de cadeia npm (ccmanager); tmux (Squad, Agent Deck) |

## Ambiente

Tudo roda num só computador Windows (192.168.100.24) com dois usuários.

| Item | Valor |
|---|---|
| Usuário principal | Gabriel Sanga: roda o painel; tem acesso aos servidores da empresa |
| Usuário IA | Conta local `ia`, sem administrador; rede só para TFS, APIs da Anthropic e da OpenAI e NuGet; tem Claude Code e Codex instalados e logados |
| Repositórios | `C:\Fontes` (acessível ao IA). Recomendado migrar para um clone próprio do IA, ex.: `C:\Fontes-IA` |
| Servidor SSH | OpenSSH Server do Windows; `ListenAddress 127.0.0.1`; `AllowUsers ia`; só chave; túneis e redirecionamentos desligados; regra de firewall desativada |
| Chave | `id_ed25519` do usuário principal, cadastrada em `C:\Users\IA\.ssh\authorized_keys` |
| Atalho manual | Perfil do Windows Terminal "Claude (IA)": `ssh -t ia@localhost powershell -NoExit -Command Set-Location C:\Fontes` |
| Configuração dos agentes | `C:\Users\IA\.claude\` e `C:\Users\IA\.codex\` |
| Pasta de status (a criar) | `C:\ProgramData\Painel\sessoes`: IA escreve, usuário principal só lê |
| Scripts do lado do IA (a criar) | `C:\Painel\hook.ps1` e `C:\Painel\abrir.ps1`: legíveis pelo IA, editáveis só pelo usuário principal |
| Dados do painel (a criar) | `%APPDATA%\Painel\demandas.json` do usuário principal: só ele lê e escreve |

Permissões da pasta de status (PowerShell como administrador):

```powershell
New-Item -ItemType Directory -Force C:\ProgramData\Painel\sessoes | Out-Null
icacls C:\ProgramData\Painel /inheritance:r /grant "SYSTEM:(OI)(CI)F" /grant "Administradores:(OI)(CI)F" /grant "ia:(OI)(CI)M" /grant "<seu-usuario>:(OI)(CI)RX"
```

O guia de instalação do SSH (passos 0 a 6) está no arquivo `guia-ssh-usuario-ia.html`. Em Windows em inglês, o grupo é `Administrators`.

## Arquitetura

O painel só lê o que o IA escreve; os agentes rodam isolados.

```
  USUARIO PRINCIPAL            ARQUIVOS COMPARTILHADOS         USUARIO IA (sem admin)

  +------------------+                                         +------------------+
  | demandas.json    |         +------------------+    grava   | hook.ps1         |
  | demandas, repos, |         | Pasta de status  |<-----------| grava status,    |
  | sessoes          |         | IA escreve,      |            | sem saida        |
  +------------------+         | painel le        |            +------------------+
        ^                      +------------------+                     ^
        | le e grava                    |                               | eventos
        v                               v                               |
  +------------------+         +------------------+    edita   +------------------+
  | Painel WPF       |<--------| C:\Fontes        |<-----------| Claude Code /    |
  | status, OpenSpec,|   le    | codigo e         |            | Codex            |
  | botoes           |         | tasks.md         |            | rodam como IA    |
  +------------------+         +------------------+            +------------------+
        |                                                               ^
        | Abrir                                                         |
        v                                                               |
  +------------------+              ssh ia@localhost           +------------------+
  | Windows Terminal |---------------------------------------->| abrir.ps1        |
  | uma aba por      |                                         | valida e inicia  |
  | sessao           |                                         | o agente         |
  +------------------+                                         +------------------+
```

Arquitetura do MVP: 2 usuários, 1 pasta de status.

O painel lê a pasta de status e o `tasks.md`, mas monta comandos só a partir do `demandas.json`. O único caminho do painel até os agentes é a aba do Windows Terminal com `ssh ia@localhost`, que chama o `abrir.ps1`.

## Modelo de dados e contratos

Cinco contratos ligam as partes. Os comandos marcados "a confirmar" devem ser checados com `claude --help` e `codex --help` na versão instalada.

### 1. demandas.json (usuário principal)

Fonte única para montar comandos. Só o usuário principal escreve.

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
            { "agente": "claude", "sessionId": "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10", "rotulo": "implementação", "criadaEm": "2026-11-13T10:05:00-03:00" }
          ]
        }
      ]
    }
  ]
}
```

`pasta` é relativa à raiz dos fontes do IA e só aceita `[A-Za-z0-9_.-]`.

### 2. Arquivo de status (escrito pelo IA)

Um arquivo por sessão em `C:\ProgramData\Painel\sessoes\<session_id>.json`. O painel só exibe esses dados; nunca os usa para montar comandos.

```json
{"agente":"claude","session_id":"0b6f1c2e-...","cwd":"C:\\Fontes\\Faturamento","status":"aguardando","evento":"Stop","atualizado":"2026-11-13T10:22:31-03:00"}
```

| Evento do hook | Status | Claude | Codex |
|---|---|---|---|
| `SessionStart` | ativa | sim | sim |
| `UserPromptSubmit` | executando | sim | sim |
| `Stop` | aguardando você | sim | sim |
| `Notification` | precisa de atenção | sim | a confirmar |
| `SessionEnd` | encerrada | sim | a confirmar |
| sem atualização há mais de 15 min (status ativa ou executando) | inativa (calculado pelo painel) | — | — |

### 3. hook.ps1 (roda como IA)

Regras obrigatórias: nunca escrever na saída padrão (em `SessionStart` e `UserPromptSubmit` a saída vira contexto do modelo), sempre terminar com código 0 (no Claude, código 2 bloqueia a ação) e ser rápido.

```powershell
param([string]$Agente = 'claude')
try {
  $in = [Console]::In.ReadToEnd() | ConvertFrom-Json
  $id = [string]$in.session_id
  if ($id -notmatch '^[A-Za-z0-9_-]{1,100}$') { exit 0 }
  $status = switch ([string]$in.hook_event_name) {
    'SessionStart' {'ativa'} 'UserPromptSubmit' {'executando'}
    'Stop' {'aguardando'} 'Notification' {'atencao'} 'SessionEnd' {'encerrada'}
    default { $null } }
  if ($status) {
    $dir = 'C:\ProgramData\Painel\sessoes'
    $obj = [ordered]@{ agente = $Agente; session_id = $id; cwd = [string]$in.cwd
      status = $status; evento = [string]$in.hook_event_name; atualizado = (Get-Date).ToString('o') }
    $tmp = Join-Path $dir "$id.tmp"
    $obj | ConvertTo-Json -Compress | Set-Content -Path $tmp -Encoding UTF8
    Move-Item -Force $tmp (Join-Path $dir "$id.json")
  }
} catch { }
exit 0
```

Configuração no Claude Code (`C:\Users\IA\.claude\settings.json`), repetindo o mesmo bloco para `UserPromptSubmit`, `Stop`, `Notification` e `SessionEnd`:

```json
{
  "hooks": {
    "SessionStart": [
      { "hooks": [ { "type": "command", "command": "powershell -NoProfile -ExecutionPolicy Bypass -File C:\\Painel\\hook.ps1 -Agente claude" } ] }
    ]
  }
}
```

Configuração no Codex (`C:\Users\IA\.codex\hooks.json`), mesmo formato, para `SessionStart`, `UserPromptSubmit` e `Stop`, trocando `-Agente codex`. O Codex pode pedir para revisar e confiar nos hooks pelo comando `/hooks` na primeira execução.

### 4. abrir.ps1 (roda como IA, chamado via SSH)

Recebe só parâmetros validados; evita aspas aninhadas na linha do SSH.

```powershell
param(
  [Parameter(Mandatory)][ValidateSet('claude','codex')][string]$Agente,
  [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9_.-]+$')][string]$Repo,
  [ValidatePattern('^[0-9a-fA-F-]{36}$')][string]$Sessao,
  [ValidateSet('novo','retomar')][string]$Modo = 'novo'
)
$pasta = Join-Path 'C:\Fontes' $Repo
if (-not (Test-Path $pasta)) { Write-Host "Repositório não encontrado: $pasta"; exit 1 }
Set-Location $pasta
switch ("$Agente-$Modo") {
  'claude-novo'    { claude --session-id $Sessao }   # a confirmar
  'claude-retomar' { claude --resume $Sessao }
  'codex-novo'     { codex }
  'codex-retomar'  { codex resume $Sessao }          # a confirmar
}
```

No Claude, o painel gera o GUID da sessão antes de abrir. No Codex, o ID só aparece no hook `SessionStart`: o painel mostra a sessão nova como "não vinculada" naquele repositório e o usuário vincula com um clique.

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
wt -w painel nt --title "CHG-142 · Faturamento" --suppressApplicationTitle ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1 -Agente claude -Repo Faturamento -Sessao <guid> -Modo retomar
```

- `-w painel` junta todas as abas dos agentes numa janela separada dos terminais do dia a dia.
- O painel monta a linha apenas com valores do `demandas.json`, passados como argumentos separados (`ProcessStartInfo.ArgumentList`), nunca concatenando texto vindo de arquivos de status.
- **Limitação:** o Windows Terminal não permite focar uma aba já aberta. Se a sessão estiver ativa, o painel avisa "já aberta no terminal" e pede confirmação antes de abrir outra aba.

**Stack sugerida:** .NET 8, WPF, CommunityToolkit.Mvvm, System.Text.Json, `FileSystemWatcher` com releitura periódica a cada 5 s como garantia, um pacote de notificações/ícone de bandeja para WPF (ex.: Hardcodet.NotifyIcon.Wpf).

## Requisitos funcionais por etapa

Cada etapa entrega algo usável no dia a dia; só avance quando a anterior estiver em uso.

| Etapa | Entrega | Critério de aceite |
|---|---|---|
| 0. Pré-requisito | SSH local + usuário IA (feito) | `ssh ia@localhost` abre terminal como IA; servidores importantes inacessíveis a partir dele |
| 1. Scripts | Pasta de status com permissões; `hook.ps1`; hooks configurados no Claude e no Codex; `abrir.ps1` | Abrir e usar uma sessão de cada agente gera e atualiza o JSON de status; o agente não mostra nada estranho no contexto; `abrir.ps1` funciona chamado à mão via `ssh` |
| 2. Tela de sessões | Solution com `Painel.Core` + `Painel.Wpf`; lista de todas as sessões lidas da pasta de status, com cor por status e regra de "inativa" | Status muda na tela em até 5 s após o evento; arquivos inválidos são ignorados sem travar |
| 3. Abrir e retomar | Botões que montam o comando `wt` + `ssh` + `abrir.ps1` | Nova sessão do Claude abre com GUID gerado pelo painel; retomar abre a conversa certa; aba com título da demanda |
| 4. Demandas | `demandas.json`; criar, editar e arquivar demandas; vincular repositórios e sessões (inclusive sessões não vinculadas do Codex) | Uma demanda com 2 repositórios e sessões dos 2 agentes aparece agrupada |
| 5. OpenSpec e avisos | Barra de progresso do `tasks.md`; ícone na bandeja; notificação em "atenção" e "aguardando" | Progresso bate com o `tasks.md`; notificação aparece com o painel minimizado |

Testes unitários em `Painel.Core.Tests` para: parser do `tasks.md`, mapeamento evento → status, regra de inatividade, validação de GUID e nome de pasta, montagem dos argumentos do `wt`.

## Requisitos de segurança

O painel roda como o usuário principal, com acesso aos servidores da empresa. Nada escrito pelo usuário IA pode virar uma ação executada como o usuário principal.

1. **RS-1:** arquivos de status são apenas dados para exibir. Nunca montar comandos, caminhos ou argumentos a partir deles.
2. **RS-2:** comandos são montados só com valores do `demandas.json`, passados como argumentos separados; sem shell intermediário do lado do usuário principal.
3. **RS-3:** validar `sessionId` (GUID) e `pasta` (`[A-Za-z0-9_.-]`) no painel e de novo no `abrir.ps1`.
4. **RS-4:** o painel nunca escreve na pasta de status; o IA nunca escreve no `demandas.json` nem nos scripts `C:\Painel\*.ps1`.
5. **RS-5:** ler arquivos de status com limite de tamanho (ex.: 16 KB) e ignorar os inválidos.
6. **RS-6:** hooks sem saída padrão e com `exit 0` sempre; sem segredos nos arquivos de status.
7. **RS-7:** no container (padrão), o agente roda sem privilégios nem capabilities, numa rede sem saída direta; só vê `C:\Fontes`, `C:\openspec\ERP`, a pasta de status e os próprios volumes, nunca o socket do Docker nem o perfil do Windows; o proxy libera só a lista de destinos; scripts e hooks do Claude ficam na imagem, sem escrita para o agente; o nome do container é validado (`[A-Za-z0-9][A-Za-z0-9_.-]{0,62}`) antes de entrar no comando. No modo Windows, com o SSH ligado, mantê-lo como configurado (só `localhost`, usuário isolado `ia` por padrão e configurável no painel, só chave, sem túneis); chave com passphrase + `ssh-agent`; Windows Update em dia. O host é sempre `localhost`. Com "Abrir pelo SSH" desligado o agente roda sem o isolamento, com os acessos de quem usa o painel; o usuário informado é validado (`[A-Za-z0-9_][A-Za-z0-9_.-]{0,19}`, nunca começa com `-`) antes de entrar no comando.
8. **RS-8:** o IA deve usar um clone próprio; o código chega ao usuário principal só via TFS (push em branch, revisão no PR, pull). Enquanto a pasta for compartilhada, revisar o diff antes de compilar como usuário principal, com atenção a `.csproj`, `Directory.Build.props`, `.targets`, `nuget.config`, scripts `.ps1`, `.git/hooks` e `.vscode/tasks.json`.
9. **RS-9:** apresentar o desenho à TI ou à segurança da empresa (serviço SSH ativo pode ser sinalizado pelo EDR ou violar política).

## Riscos, limitações e questões em aberto

| Risco ou limitação | Impacto | Mitigação |
|---|---|---|
| Fechar a aba encerra a sessão SSH e o agente | Trabalho em andamento para | Conversa recuperada com `--resume`; sessões persistentes ficam para depois do MVP |
| `SessionEnd` não dispara ao fechar à força | Status preso em "executando" | Regra de inatividade após 15 min sem atualização |
| Via SSH: sem colar imagem, sem janelas gráficas, navegador não abre sozinho, alguns atalhos (Shift+Enter) podem falhar | Pequenas fricções | `dotnet build/test` funcionam; login copiando o link; `\` + Enter para quebrar linha |
| Atraso do PowerShell em cada hook | Alguns décimos de segundo por evento | Aceitável no MVP; depois, trocar por executável .NET com AOT |
| Hooks do Codex mudam entre versões | Status do Codex pode quebrar | Fixar a versão; testar após atualizar |
| Apps oficiais evoluem e passam a cobrir o caso | Painel perde valor | Manter o escopo pequeno; reavaliar antes de cada etapa |
| Política da empresa sobre SSH | Ter que desmontar o isolamento | Aprovação da TI antes de investir no painel |

### Questões em aberto

- [ ] A versão instalada do Claude Code aceita `claude --session-id <guid>`?
- [ ] Como o Codex retoma uma sessão por ID (`codex resume <id>`)? Ele tem `Notification` e `SessionEnd`?
- [ ] Os hooks do Codex disparam também no app do Codex, ou só na CLI?
- [ ] Sessões do `claude agents` (segundo plano) sobrevivem à queda da conexão SSH?
- [ ] Migrar o IA para um clone próprio (`C:\Fontes-IA`) ou manter `C:\Fontes` compartilhado?
- [ ] O app Claude Desktop conecta por SSH em `ia@localhost`? Se sim, as sessões dele também alimentam o painel pelos hooks.
- [ ] Tempo de inatividade ideal (15 min é um chute inicial).

## Como continuar numa nova sessão

Exporte este documento em Markdown e salve como `docs/PRD.md` na pasta do projeto do painel (ou cole o conteúdo na conversa). Depois, comece a sessão com:

```
Leia o docs/PRD.md. Ele descreve o Painel de Demandas, um app WPF (.NET 8) que organiza
sessões do Claude Code e do Codex por demanda, lê status gravado por hooks e abre as
sessões no Windows Terminal via SSH no usuário isolado "ia".

O SSH local já está configurado e funcionando. Vamos começar pela Etapa <N>.
Antes de escrever código:
1. Confirme as questões em aberto que afetam essa etapa.
2. Siga os contratos da seção "Modelo de dados e contratos" sem mudar nomes de campos.
3. Respeite todos os requisitos de segurança (RS-1 a RS-9).
4. Proponha a estrutura de arquivos e espere minha aprovação.
```

### Próximos passos

1. Usar `claude agents` e `codex resume` dentro do usuário IA por cerca de duas semanas e anotar o que faz falta.
2. Responder as questões em aberto que dá para testar à mão (`--session-id`, `codex resume`, queda do SSH).
3. Se o painel ainda fizer sentido: Etapa 1 (scripts), depois Etapa 2 (tela de sessões).

### Estrutura sugerida do repositório do painel

```
Painel/
  docs/PRD.md
  scripts/hook.ps1             -> copiado para C:\Painel\
  scripts/abrir.ps1            -> copiado para C:\Painel\
  src/Painel.Core/             modelos, leitura de JSON, parser do OpenSpec, montagem de comandos
  src/Painel.Wpf/              telas (MVVM)
  tests/Painel.Core.Tests/
```
