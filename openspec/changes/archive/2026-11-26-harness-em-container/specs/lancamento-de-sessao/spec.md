## ADDED Requirements

### Requirement: Ambiente de execução da sessão
Uma sessão nova SHALL abrir no ambiente escolhido em "Executar os agentes em" no momento da abertura. A retomada de uma sessão registrada e a nova abertura de um lançamento pendente SHALL usar o ambiente guardado no registro, mesmo que a configuração em uso seja outra. Uma sessão sem registro, retomada a partir de "Sessões", SHALL abrir no ambiente em uso.

#### Scenario: Sessão nova com o container escolhido
- **WHEN** "Container Docker" está escolhido e o usuário abre uma sessão nova do Claude em `Faturamento`
- **THEN** a aba abre o Claude Code dentro do container, em `/workspace/Faturamento`, e o registro guarda o ambiente `container`

#### Scenario: Retomar sessão registrada no container com Windows escolhido
- **WHEN** "Windows" está escolhido e o usuário retoma uma sessão registrada no ambiente `container`
- **THEN** a aba retoma a conversa dentro do container

#### Scenario: Registro de uma versão anterior
- **WHEN** o usuário retoma uma sessão registrada antes desta versão, sem ambiente guardado
- **THEN** a aba retoma a conversa no Windows

### Requirement: Composição do comando no container
Para sessões do ambiente `container`, o painel SHALL iniciar `wt.exe` com os argumentos, nesta ordem e cada um separado: `-w`, `painel`, `nt`, `--title`, o título, `--suppressApplicationTitle`, `docker`, `exec`, `--interactive`, `--tty`, o nome do container, `/opt/painel/abrir.sh`, `--agente`, o agente, `--repo` e a pasta (ou, para um repositório cadastrado por caminho completo, `--caminho` e o caminho no container em Base64), `--sessao` e o identificador quando houver, `--modo` e o modo. Nenhum shell SHALL intermediar a chamada.

#### Scenario: Nova sessão do Claude no container
- **WHEN** o painel abre a sessão nova `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10` do Claude em `Faturamento`, com o container `datapilot-agente`
- **THEN** depois de `--suppressApplicationTitle` vêm `docker`, `exec`, `--interactive`, `--tty`, `datapilot-agente`, `/opt/painel/abrir.sh`, `--agente`, `claude`, `--repo`, `Faturamento`, `--sessao`, `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10`, `--modo`, `novo`

#### Scenario: Nova sessão do Codex no container
- **WHEN** o painel abre uma sessão nova do Codex em `Faturamento` no container
- **THEN** os argumentos terminam em `--agente`, `codex`, `--repo`, `Faturamento`, `--modo`, `novo`, sem `--sessao`

#### Scenario: Abas reunidas
- **WHEN** o painel abre uma sessão no container e outra no Windows
- **THEN** as duas abas ficam na mesma janela do Windows Terminal, de nome `painel`

### Requirement: Locais disponíveis no container
O container SHALL oferecer dois locais: `C:\Fontes`, visto como `/workspace`, e `C:\openspec\ERP`, visto como `/openspec/ERP`, cada um com todas as pastas filhas. Para uma sessão do ambiente `container` num repositório fora desses locais, o painel SHALL NOT registrar nada nem iniciar processo, e SHALL informar quais são os locais e que o usuário pode escolher "Windows" nas configurações.

#### Scenario: Subpasta da raiz dos fontes
- **WHEN** o painel abre no container uma sessão no repositório `C:\Fontes\Grupo\Faturamento`
- **THEN** o comando leva `--caminho` com o Base64 de `/workspace/Grupo/Faturamento`

#### Scenario: Pasta do OpenSpec
- **WHEN** o painel abre no container uma sessão no repositório `C:\openspec\ERP`
- **THEN** o comando leva `--caminho` com o Base64 de `/openspec/ERP`

#### Scenario: Pasta fora dos locais
- **WHEN** "Container Docker" está escolhido e o usuário abre uma sessão no repositório `D:\Projetos\Faturamento`
- **THEN** nada é registrado, nenhum processo é iniciado e o painel informa os locais disponíveis no container

### Requirement: Ambiente do container indisponível
Antes de abrir uma sessão do ambiente `container`, o painel SHALL verificar se o Docker responde e se o container existe. Se o container existir parado, o painel SHALL iniciá-lo e seguir. Se o Docker não responder, o container não existir ou não puder ser iniciado, o painel SHALL NOT abrir a aba, SHALL desfazer o registro feito para essa abertura e SHALL mostrar uma mensagem que diga o que falhou e o que fazer. A verificação SHALL ter tempo limite e SHALL NOT travar a janela.

#### Scenario: Docker Desktop fechado
- **WHEN** o usuário abre uma sessão nova no container com o Docker Desktop fechado
- **THEN** nenhuma aba abre, nada fica registrado e o painel orienta a abrir o Docker Desktop

#### Scenario: Container parado
- **WHEN** o container `datapilot-agente` existe e está parado, e o usuário abre uma sessão nova nele
- **THEN** o painel inicia o container e a aba abre

#### Scenario: Container inexistente
- **WHEN** não existe container com o nome configurado
- **THEN** nenhuma aba abre, nada fica registrado e o painel orienta a subir o ambiente com `docker compose up -d` ou a escolher "Windows" nas configurações

## MODIFIED Requirements

### Requirement: Nova sessão
O painel SHALL oferecer a ação de abrir uma sessão nova no card de cada repositório de uma demanda e na área "Sessões", em que o usuário escolhe o agente e, opcionalmente, um rótulo; em "Sessões", escolhe antes a pasta do repositório. A ação SHALL abrir uma aba do Windows Terminal que inicia o agente nesse repositório, no ambiente de execução da sessão. Uma sessão nova do Claude SHALL ser registrada nesse repositório dessa demanda, ou da `sem-demanda`.

#### Scenario: Nova sessão do Claude
- **WHEN** o usuário aciona "Nova sessão" no card `Faturamento` de um trabalho, escolhe o agente Claude e confirma
- **THEN** abre uma aba do Windows Terminal com o Claude Code iniciado em `Faturamento`, no ambiente de execução em uso, e a sessão fica registrada em `Faturamento` dentro desse trabalho

#### Scenario: Nova sessão do Codex
- **WHEN** o usuário aciona "Nova sessão" no card `Faturamento`, escolhe o agente Codex e confirma
- **THEN** abre uma aba do Windows Terminal com o Codex iniciado em `Faturamento`, no ambiente de execução em uso

#### Scenario: Nenhum repositório cadastrado
- **WHEN** o trabalho selecionado não tem repositórios vinculados
- **THEN** nenhuma ação de "Nova sessão" é oferecida no trabalho e o painel orienta a vincular um repositório

#### Scenario: Demanda arquivada
- **WHEN** o usuário visualiza um trabalho arquivado
- **THEN** os cards dele não oferecem "Nova sessão"

#### Scenario: Nova sessão em "Sessões"
- **WHEN** o usuário aciona "Nova sessão" em "Sessões", escolhe a pasta `Estoque` e o Claude
- **THEN** o repositório `Estoque` é vinculado à `sem-demanda`, se ainda não estiver, e a sessão abre e fica registrada nele

### Requirement: Composição do comando
Para sessões do ambiente `windows`, o painel SHALL iniciar `wt.exe` com os argumentos, nesta ordem e cada um separado: `-w`, `painel`, `nt`, `--title`, o título, `--suppressApplicationTitle`; com "Abrir pelo SSH" ligado, `ssh`, `-t` e `<usuário>@localhost`; depois `powershell`, `-NoProfile`, `-ExecutionPolicy`, `Bypass`, `-File`, `C:\Painel\abrir.ps1`, `-Agente`, o agente, `-Repo`, a pasta, `-Sessao` e o identificador quando houver, `-Modo` e o modo. O usuário SHALL ser validado de novo antes de entrar no comando. Nenhum shell SHALL intermediar a chamada.

#### Scenario: Retomada do Claude
- **WHEN** o painel retoma a sessão `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10` do Claude em `Faturamento`, registrada no Windows
- **THEN** os argumentos terminam em `-Agente`, `claude`, `-Repo`, `Faturamento`, `-Sessao`, `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10`, `-Modo`, `retomar`

#### Scenario: Nova sessão do Codex
- **WHEN** o painel abre uma sessão nova do Codex em `Faturamento` no Windows
- **THEN** os argumentos terminam em `-Agente`, `codex`, `-Repo`, `Faturamento`, `-Modo`, `novo`, sem `-Sessao`

#### Scenario: Abas reunidas
- **WHEN** o painel abre duas sessões
- **THEN** as duas abas ficam na mesma janela do Windows Terminal, de nome `painel`

#### Scenario: Pelo SSH com o usuário padrão
- **WHEN** o painel abre uma sessão no Windows com "Abrir pelo SSH" ligado e o usuário `ia`
- **THEN** depois de `--suppressApplicationTitle` vêm `ssh`, `-t`, `ia@localhost` e `powershell`

#### Scenario: Pelo SSH com outro usuário
- **WHEN** o painel abre uma sessão no Windows com "Abrir pelo SSH" ligado e o usuário `agente-ia`
- **THEN** o destino do `ssh` é `agente-ia@localhost`

#### Scenario: Sem SSH
- **WHEN** o painel abre uma sessão no Windows com "Abrir pelo SSH" desligado
- **THEN** depois de `--suppressApplicationTitle` vem `powershell`, sem `ssh`, `-t` nem destino

### Requirement: Validação imediatamente antes de lançar
Antes de iniciar o terminal, o painel SHALL validar de novo o agente (`claude` ou `codex`), a pasta (regra do cadastro), o identificador (GUID canônico) e, para o ambiente `container`, o nome do container (regra das configurações). Se algum valor for inválido, o painel SHALL NOT iniciar nenhum processo e SHALL informar o usuário.

#### Scenario: Valor inválido chega ao lançamento
- **WHEN** a pasta ou o identificador de uma sessão não passa na validação no momento de lançar
- **THEN** nenhum processo é iniciado e o painel mostra uma mensagem de erro

#### Scenario: Nome de container inválido no lançamento
- **WHEN** o nome do container em uso não passa na validação no momento de lançar uma sessão do ambiente `container`
- **THEN** nenhum processo é iniciado, nem mesmo a verificação do Docker, e o painel mostra uma mensagem de erro
