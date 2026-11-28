# Design

## Context

A motivação está em proposal.md. O estado atual, já com `configuracoes-do-painel` e `scripts-do-painel` aplicadas:

- **`AberturaSessao`:** recarrega o `demandas.json` a cada operação, registra a sessão ou o lançamento e chama `IDisparador.Iniciar(ComandoTerminal)`. Se o disparo lança `FalhaDisparoException`, o registro é desfeito. Esse fluxo de desfazer já existe e serve também para a falha de container indisponível.
- **`MontadorComando`:** monta `wt.exe … [ssh -t <usuário>@localhost] powershell -File C:\Painel\abrir.ps1 -Agente … -Repo … [-Sessao …] -Modo …` como função pura, revalidando cada valor.
- **Status:** os hooks chamam `C:\Painel\hook.ps1`, que grava `C:\ProgramData\Painel\sessoes\<id>.json` por temporário e troca (`File.Replace`). O `MonitorPasta` e o `LeitorPasta` leem a pasta, e o `VinculoAutomatico` compara o último segmento do `cwd` com a pasta do repositório (`TextoExibicao.NomePasta` aceita `/` e `\`). Também compara a **data de criação** do arquivo de status com a data do lançamento.
- **Repositório:** é sempre uma pasta filha direta de `C:\Fontes` (`ValidadorEntrada.PastaValida`), o que mapeia diretamente para `/workspace/<pasta>`.

## Goals / Non-Goals

**Goals:**
- Sessões novas rodam num container sem rota de rede própria, sem acesso ao perfil e às credenciais do Windows, com saída só para os destinos liberados.
- Reaproveitar o Core quase inteiro: o contrato do arquivo de status, o vínculo, a exibição e o registro continuam os mesmos.
- Sessões já registradas continuam retomando como antes.
- Nenhum valor configurado ou registrado entra num comando sem validação.

**Non-Goals:**
- Terminal dentro do painel e sessões que sobrevivem ao fechamento da aba (change `terminal-embutido`).
- Proteger o `.git` contra escrita do agente.
- Liberar o TFS ou outros servidores internos. Fica só como exemplo comentado no `squid.conf`.
- Migrar conversas antigas para o container. As pastas de conversa dos agentes dependem do caminho do repositório (`C:\Fontes\X` contra `/workspace/X`), então a migração não seria confiável.
- Remover o modo Windows e o SSH. Eles continuam como alternativa e podem sair numa change futura.

## Decisions

### 1. Container de longa duração e `docker exec` por sessão

O compose sobe um container `datapilot-agente`, que fica rodando (`sleep infinity`). Cada sessão é um `docker exec --interactive --tty` nele.

Por quê:
- a abertura é rápida;
- o login e as conversas ficam num só lugar;
- é a base de que a change `terminal-embutido` precisa, com tmux no mesmo container.

Alternativa considerada: um container por sessão (`docker run --rm`). Rejeitada porque cada sessão pagaria a criação do container, e os volumes de login seriam disputados por vários containers.

### 2. Rede: `internal` mais proxy com lista de destinos

```
agente ──(rede "interna", internal: true)──► proxy (squid) ──(rede "externa")──► internet
```

- O agente só enxerga a rede `interna`, que não tem rota de saída. Ignorar `HTTPS_PROXY` não adianta, porque não há para onde ir.
- O squid libera por domínio (`dstdomain`) e recusa todo o resto (`http_access deny all`). Com HTTPS, o squid vê só o host do `CONNECT`, e é isso que basta para a lista. Não há interceptação de TLS.
- A lista padrão cobre a API, a autenticação e a telemetria essencial do Claude Code e do Codex. Os domínios exatos são confirmados na tarefa 1.1, pelo `access.log` do squid, porque mudam entre versões dos CLIs.
- O TFS entra como exemplo comentado, com a observação de que a garantia de "somente leitura" vem do PAT, e não do proxy.

Alternativa considerada: firewall `iptables` dentro do container, como no devcontainer de referência do Claude Code. Rejeitada porque exige `NET_ADMIN` no container e porque a regra fica no mesmo lugar que o agente.

### 3. Montagens e usuário

| Windows | Container | Acesso |
|---|---|---|
| `C:\Fontes` (raiz dos fontes) | `/workspace` | leitura e escrita |
| `C:\ProgramData\Painel\sessoes` | `/status` | leitura e escrita |
| `C:\openspec\ERP` | `/openspec/ERP` | leitura e escrita |
| volume `datapilot-claude` | `/home/node/.claude` | do agente |
| volume `datapilot-codex` | `/home/node/.codex` | do agente |

- **Usuário:** `node` (uid 1000) da imagem `node`, com `cap_drop: [ALL]` e `security_opt: [no-new-privileges:true]`.
- **O que não é montado:** o socket do Docker e qualquer pasta do perfil do Windows.
- **Configuração do Claude:** `CLAUDE_CONFIG_DIR=/home/node/.claude`, para que toda a configuração do Claude fique no volume. Isso inclui o arquivo que, sem essa variável, iria para `~/.claude.json`.
- **Atualizações:** `DISABLE_AUTOUPDATER=1`, porque os CLIs são atualizados reconstruindo a imagem.
- **Raiz dos fontes:** o caminho vem de uma variável do compose (`RAIZ_FONTES`, padrão `C:\Fontes`), coerente com `--raiz-fontes` do painel.

### 4. Scripts no container: `abrir.sh` em bash e `hook.mjs` em Node

- **`abrir.sh`:** é um porte direto do `abrir.ps1`, com as mesmas regras. Os parâmetros passam a ter `--`, as regex são as mesmas e a raiz `/workspace` fica fixa no script. O script termina com `exec` no agente, para que os sinais do terminal cheguem a ele.
  - A segunda tentativa do Claude (retomada que falha em menos de 30 s) não pode usar `exec` na primeira chamada. Ela segue a mesma lógica do `.ps1`: mede o tempo, confere o código de saída e só então tenta de novo.
- **`hook.mjs`:** é escrito em Node, que já está na imagem por causa dos CLIs. Assim não depende de `jq` nem de PowerShell. Ele cumpre todo o contrato de `status-de-sessao`:
  - só os seis campos;
  - não regredir para um status mais antigo;
  - temporário e `rename` na mesma pasta;
  - silêncio na saída padrão;
  - sempre terminar com código 0.
- **Onde ficam:** os dois scripts ficam em `/opt/painel`, com dono `root` e modo `0755` na pasta e `0555` nos arquivos.
- **Testes:** os casos do `scripts/testes/verificar.ps1` que valem para os scripts do container são portados para `docker/testes/verificar.sh`, que roda dentro do container com `claude` e `codex` falsos à frente no `PATH`.

Alternativa considerada: instalar o `pwsh` na imagem e reaproveitar os `.ps1`. Rejeitada porque os scripts têm caminhos e chamadas do Windows (`File.Replace` com preservação da data de criação, `Get-Item` com `Parent`) que precisariam mudar de qualquer jeito, e porque o `pwsh` aumentaria bastante a imagem.

### 5. Hooks configurados pela imagem

- **Claude Code:** a configuração gerenciada fica em `/etc/claude-code/managed-settings.json`, com dono `root` e somente leitura. Ela tem precedência sobre a configuração do usuário, e o agente não consegue alterá-la nem desligar os hooks.
- **Codex:** o entrypoint grava `~/.codex/hooks.json` a partir de um modelo da imagem a cada início, se o conteúdo for diferente. O Codex exige confiar nos hooks uma vez, pelo `/hooks`. Como a confiança é ligada ao conteúdo, só precisa ser renovada quando o hook mudar. O guia de instalação registra esse passo.

### 6. Fuso horário

O compose repassa `TZ` (padrão `America/Sao_Paulo`). O `hook.mjs` grava `atualizado` com o deslocamento local (`+/-HH:MM`), no mesmo formato do `hook.ps1`. O `ValidadorStatus` já aceita `Z` e deslocamentos, e o instante é o mesmo nos dois casos.

### 7. Data de criação do arquivo de status pelo container

**Risco principal desta change.** O `VinculoAutomatico` usa a data de criação do arquivo de status, vinda do sistema de arquivos, para casar a sessão com o lançamento. No Windows, `File.Replace` preserva essa data. No container, a troca é um `rename` sobre a montagem do Docker Desktop.

- **Expectativa:** a data deve ser preservada pelo *tunneling* do NTFS, que devolve a data de criação a um nome excluído ou renomeado e recriado em até 15 s.
- **Por que isso ainda é incerto:** a operação passa pela camada de compartilhamento de arquivos do Docker Desktop, então precisa ser medida.
- **Como verificar:** a tarefa 1.2 grava `SessionStart` e, 20 s depois, `Stop` pela montagem, e compara as datas.
- **Plano B, se falhar:** o hook cria uma única vez, com `O_EXCL`, um marcador `<id>.criado`, que nunca é regravado, e a leitura usa a data de criação dele. Esse caminho muda a capability `leitura-de-status`, então, se for preciso, esta change ganha o delta correspondente antes da implementação do grupo 3.
- **Resultado (26/11/2026, Docker 29.5.3):** a data **não** é preservada. Depois do `rename` pela montagem, a data de criação passa a ser a da segunda gravação. **Vale o plano B**: delta em `leitura-de-status` e em `status-de-sessao`, tarefas 2.4 e 3.4. O marcador é criado antes do arquivo de status e, se não puder ser criado, o status não é gravado, para o painel nunca tomar uma data posterior como a de início da sessão. A data do marcador se manteve entre as gravações.

### 8. Configurações e ambiente por sessão

- **`Configuracoes`:** passa a ser `Configuracoes(Execucao Execucao, string Container, bool UsarSsh, string UsuarioSsh)`, com o enum `Execucao { Container, Windows }`.
  - No JSON, a seção fica `execucao: "container" | "windows"` e `container: "<nome>"`.
  - Campo ausente ou inválido vale o padrão, como já acontece com os campos de SSH.
- **Validação:** `ValidadorEntrada.ContainerValido` aceita `^[A-Za-z0-9][A-Za-z0-9_.-]{0,62}$`. Começar por letra ou dígito impede que o valor vire opção do `docker`, como `--privileged`.
- **Registros:** `SessaoRegistrada` e `LancamentoPendente` ganham `Execucao`.
  - No JSON, o campo é `execucao` em cada sessão e lançamento.
  - `DadosPainel` grava o campo sempre e, na carga, trata ausente como `windows` e valor desconhecido como registro inválido.
- **Origem do ambiente:**
  - `AberturaSessao.Nova` usa o `Execucao` das configurações;
  - `Retomar` e `ReabrirLancamento` usam o do registro;
  - `RetomarNaoRegistrada` usa o das configurações e grava esse valor no registro;
  - `VinculoAutomatico` copia o do lançamento para a sessão;
  - o vínculo manual de uma sessão já registrada em "Sessões" mantém o valor dela.

### 9. Comando e verificação do container

- **`MontadorComando`:** escolhe o formato pelo `Execucao` da sessão. Para `container`, monta `wt.exe -w painel nt --title <título> --suppressApplicationTitle docker exec --interactive --tty <container> /opt/painel/abrir.sh --agente … --repo … [--sessao …] --modo …`.
  - Não usa `--user`, porque o padrão da imagem já é `node`.
  - O título continua passando por `MontadorComando.Titulo`, que troca `;`.
  - `ComandoTerminal` ganha a propriedade `Container` (nula no modo Windows) para o disparador saber o que verificar.
- **Disparador:** um novo `DisparadorContainer` decora o `DisparadorProcesso`. Antes de chamar o `wt.exe`, ele:
  1. roda `docker inspect --format {{.State.Running}} <container>` com `ArgumentList`, sem shell, e tempo limite de 10 s;
  2. se o resultado for `false`, roda `docker start <container>` e espera até 20 s;
  3. se o `docker.exe` não existir, o Docker não responder ou o container não existir (saída de erro "No such object"), lança `FalhaDisparoException` com a mensagem de orientação, e o fluxo existente da `AberturaSessao` desfaz o registro.
- **Interface sem travar:** a verificação roda fora da thread de interface. O `MainViewModel` já chama a abertura por comando; essa chamada passa a ser `async`, com o botão desabilitado enquanto a verificação está em andamento.

### 10. `docker/` no repositório

```
docker/
  Dockerfile            # node:22-bookworm + git, ripgrep, ca-certificates; CLIs fixados por versão
  compose.yml           # serviços agente e proxy, redes interna/externa, volumes, TZ, RAIZ_FONTES
  entrypoint.sh         # mantém ~/.codex/hooks.json e executa o CMD
  proxy/squid.conf      # lista de destinos; TFS como exemplo comentado
  painel/abrir.sh
  painel/hook.mjs
  painel/claude-managed-settings.json
  painel/codex-hooks.json
  testes/verificar.sh
```

As versões do Claude Code e do Codex ficam fixadas no Dockerfile (`ARG`) e são atualizadas de forma explícita.

### 11. Dois locais disponíveis no container

Decidido pelo usuário na implementação, porque o painel passou a aceitar repositórios em qualquer pasta depois do planejamento. O container vê `C:\Fontes` (`/workspace`) e `C:\openspec\ERP` (`/openspec/ERP`), com as pastas filhas.

- Uma pasta direta de `C:\Fontes` continua indo como `--repo`. Qualquer outra pasta dos dois locais vai como `--caminho`, já traduzida para o caminho do container e em Base64, para espaços e `;` não serem reinterpretados pelo Windows Terminal.
- Fora desses locais, o painel recusa a abertura no container antes de registrar qualquer coisa.
- A tradução fica num único lugar do Core (`LocaisContainer`), usado pelo montador e pela comparação de pasta do vínculo.
- Se `C:\openspec\ERP` não existir, o Docker a cria vazia ao subir o ambiente.

## Risks / Trade-offs

- **[A data de criação do arquivo de status não é preservada pelo `rename` na montagem]** → Confirmado na tarefa 1.2. Vale o plano B da decisão 7 (marcador `<id>.criado`). Os marcadores se acumulam na pasta, como os arquivos de status.
- **[O agente pode reescrever o histórico do git, porque `C:\Fontes` é montada com escrita]** → Fica fora do escopo. O guia orienta a commitar antes de trabalhar com o agente, e uma change futura pode montar o `.git` de cada repositório como somente leitura.
- **[Domínios dos CLIs mudam entre versões, e o proxy pode bloquear algo legítimo]** → O `access.log` do squid mostra o que foi recusado (`TCP_DENIED`), e o guia ensina a conferir. Os CLIs ficam fixados por versão, e a lista é revista quando a versão muda.
- **[Desempenho da montagem do Windows (projetos grandes, `node_modules`, `bin/obj`)]** → É aceito nesta change. Se ficar ruim, uma change futura pode usar volumes para as pastas de build.
- **[Sessões novas passam a abrir no container por padrão]** → Quem não instalou o ambiente recebe uma mensagem com os dois caminhos: subir o ambiente ou escolher "Windows". Nenhum registro fica para trás, porque o fluxo de desfazer já existe.
- **[O Docker Desktop precisa estar aberto]** → A verificação do container dá uma mensagem clara. Iniciar o Docker Desktop pelo painel fica fora do escopo.
- **[Licença e termos dos CLIs dentro de um container]** → É o uso previsto pelos fornecedores. O próprio Claude Code tem um devcontainer de referência.

## Migration Plan

1. Instalar o Docker Desktop (backend WSL2) e subir o ambiente com `docker compose up -d` na pasta `docker`.
2. Fazer login no Claude Code e no Codex uma vez, com `docker exec -it datapilot-agente claude` e `docker exec -it datapilot-agente codex login`. Como o retorno em `localhost` do login do Codex não funciona numa rede `internal`, use o login por código de dispositivo, se a versão fixada oferecer, ou uma chave de API. Depois, confie nos hooks do Codex pelo `/hooks`.
3. Atualizar o painel. As configurações antigas são mantidas, e `execucao` passa a valer `container`.
4. As sessões já registradas continuam retomando no Windows. O usuário `ia` e o OpenSSH Server podem ser removidos quando não houver mais sessões do Windows que interessem retomar.

**Volta atrás:** escolher "Windows" nas configurações. Nenhum dado precisa ser revertido, porque o campo `execucao` é ignorado por versões anteriores e preservado pela gravação, que mantém campos desconhecidos.
