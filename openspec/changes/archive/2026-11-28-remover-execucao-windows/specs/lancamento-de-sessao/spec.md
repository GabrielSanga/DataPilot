# Spec Delta

## MODIFIED Requirements

### Requirement: Nova sessão
O painel SHALL oferecer a ação de abrir uma sessão nova no card de cada repositório de uma demanda e na área "Sessões", em que o usuário escolhe o agente e, opcionalmente, um rótulo; em "Sessões", escolhe antes a pasta do repositório. A ação SHALL abrir uma aba do Windows Terminal que inicia o agente nesse repositório, dentro do container. Uma sessão nova do Claude SHALL ser registrada nesse repositório dessa demanda, ou da `sem-demanda`.

#### Scenario: Nova sessão do Claude
- **WHEN** o usuário aciona "Nova sessão" no card `Faturamento` de um trabalho, escolhe o agente Claude e confirma
- **THEN** abre uma aba do Windows Terminal com o Claude Code iniciado dentro do container, em `/workspace/Faturamento`, e a sessão fica registrada em `Faturamento` dentro desse trabalho

#### Scenario: Nova sessão do Codex
- **WHEN** o usuário aciona "Nova sessão" no card `Faturamento`, escolhe o agente Codex e confirma
- **THEN** abre uma aba do Windows Terminal com o Codex iniciado dentro do container, em `/workspace/Faturamento`

#### Scenario: Nenhum repositório cadastrado
- **WHEN** o trabalho selecionado não tem repositórios vinculados
- **THEN** nenhuma ação de "Nova sessão" é oferecida no trabalho e o painel orienta a vincular um repositório

#### Scenario: Demanda arquivada
- **WHEN** o usuário visualiza um trabalho arquivado
- **THEN** os cards dele não oferecem "Nova sessão"

#### Scenario: Nova sessão em "Sessões"
- **WHEN** o usuário aciona "Nova sessão" em "Sessões", escolhe a pasta `Estoque` e o Claude
- **THEN** o repositório `Estoque` é vinculado à `sem-demanda`, se ainda não estiver, e a sessão abre e fica registrada nele

### Requirement: Retomada de sessão registrada
O painel SHALL retomar uma sessão registrada abrindo uma aba do Windows Terminal que retoma, dentro do container, a conversa de mesmo identificador, no repositório em que a sessão foi registrada.

#### Scenario: Retomar sessão do Claude
- **WHEN** o usuário aciona "Retomar" numa sessão do Claude registrada
- **THEN** abre uma aba com a conversa anterior, com o histórico, no repositório registrado

### Requirement: Somente dados do painel entram no comando
Os valores de agente, pasta e identificador usados no comando SHALL vir do arquivo de dados do painel ou da escolha do usuário na tela. Nenhum valor lido de um arquivo de status SHALL entrar em argumentos, no título ou na decisão de qual pasta usar.

#### Scenario: Pasta de trabalho divergente no arquivo de status
- **WHEN** uma sessão registrada em `Faturamento` tem, no arquivo de status, um `cwd` que aponta para outra pasta
- **THEN** a retomada usa `Faturamento` como `--repo`

### Requirement: Validação imediatamente antes de lançar
Antes de iniciar o terminal, o painel SHALL validar de novo o agente (`claude` ou `codex`), a pasta (regra do cadastro), o identificador (GUID canônico) e o nome do container (regra das configurações). Se algum valor for inválido, o painel SHALL NOT iniciar nenhum processo e SHALL informar o usuário.

#### Scenario: Valor inválido chega ao lançamento
- **WHEN** a pasta ou o identificador de uma sessão não passa na validação no momento de lançar
- **THEN** nenhum processo é iniciado e o painel mostra uma mensagem de erro

#### Scenario: Nome de container inválido no lançamento
- **WHEN** o nome do container em uso não passa na validação no momento de lançar uma sessão
- **THEN** nenhum processo é iniciado, nem mesmo a verificação do Docker, e o painel mostra uma mensagem de erro

### Requirement: Composição do comando no container
O painel SHALL iniciar `wt.exe` com os argumentos, nesta ordem e cada um separado: `-w`, `painel`, `nt`, `--title`, o título, `--suppressApplicationTitle`, `docker`, `exec`, `--interactive`, `--tty`, o nome do container, `/opt/painel/abrir.sh`, `--agente`, o agente, `--repo` e a pasta (ou, para um repositório cadastrado por caminho completo, `--caminho` e o caminho no container em Base64), `--sessao` e o identificador quando houver, `--modo` e o modo. Nenhum shell SHALL intermediar a chamada.

#### Scenario: Nova sessão do Claude no container
- **WHEN** o painel abre a sessão nova `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10` do Claude em `Faturamento`, com o container `datapilot-agente`
- **THEN** depois de `--suppressApplicationTitle` vêm `docker`, `exec`, `--interactive`, `--tty`, `datapilot-agente`, `/opt/painel/abrir.sh`, `--agente`, `claude`, `--repo`, `Faturamento`, `--sessao`, `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10`, `--modo`, `novo`

#### Scenario: Nova sessão do Codex no container
- **WHEN** o painel abre uma sessão nova do Codex em `Faturamento` no container
- **THEN** os argumentos terminam em `--agente`, `codex`, `--repo`, `Faturamento`, `--modo`, `novo`, sem `--sessao`

#### Scenario: Retomada do Claude no container
- **WHEN** o painel retoma a sessão `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10` do Claude em `Faturamento`
- **THEN** os argumentos terminam em `--agente`, `claude`, `--repo`, `Faturamento`, `--sessao`, `0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10`, `--modo`, `retomar`

#### Scenario: Abas reunidas
- **WHEN** o painel abre duas sessões
- **THEN** as duas abas ficam na mesma janela do Windows Terminal, de nome `painel`

### Requirement: Locais disponíveis no container
O container SHALL oferecer dois locais: `C:\Fontes`, visto como `/workspace`, e `C:\openspec\ERP`, visto como `/openspec/ERP`, cada um com todas as pastas filhas. Para uma sessão num repositório fora desses locais, como um cadastrado por uma versão anterior, o painel SHALL NOT registrar nada nem iniciar processo, e SHALL informar quais são os locais.

#### Scenario: Subpasta da raiz dos fontes
- **WHEN** o painel abre uma sessão no repositório `C:\Fontes\Grupo\Faturamento`
- **THEN** o comando leva `--caminho` com o Base64 de `/workspace/Grupo/Faturamento`

#### Scenario: Pasta do OpenSpec
- **WHEN** o painel abre uma sessão no repositório `C:\openspec\ERP`
- **THEN** o comando leva `--caminho` com o Base64 de `/openspec/ERP`

#### Scenario: Pasta fora dos locais
- **WHEN** o usuário abre uma sessão no repositório `D:\Projetos\Faturamento`, cadastrado por uma versão anterior
- **THEN** nada é registrado, nenhum processo é iniciado e o painel informa os locais disponíveis no container, sem sugerir outro ambiente

### Requirement: Ambiente do container indisponível
Antes de abrir ou retomar uma sessão, o painel SHALL verificar se o Docker responde e se o container existe. Se o container existir parado, o painel SHALL iniciá-lo e seguir. Se o Docker não responder, o container não existir ou não puder ser iniciado, o painel SHALL NOT abrir a aba, SHALL desfazer o registro feito para essa abertura e SHALL mostrar uma mensagem que diga o que falhou e o que fazer. A verificação SHALL ter tempo limite e SHALL NOT travar a janela.

#### Scenario: Docker Desktop fechado
- **WHEN** o usuário abre uma sessão nova com o Docker Desktop fechado
- **THEN** nenhuma aba abre, nada fica registrado e o painel orienta a abrir o Docker Desktop

#### Scenario: Container parado
- **WHEN** o container `datapilot-agente` existe e está parado, e o usuário abre uma sessão nova nele
- **THEN** o painel inicia o container e a aba abre

#### Scenario: Container inexistente
- **WHEN** não existe container com o nome configurado
- **THEN** nenhuma aba abre, nada fica registrado e o painel orienta a subir o ambiente com `docker compose up -d`, sem sugerir outro ambiente

## REMOVED Requirements

### Requirement: Composição do comando
**Reason**: Descrevia o comando do Windows (`ssh` e `powershell` com o `abrir.ps1`), que foi removido.
**Migration**: Todo lançamento usa o comando do requisito "Composição do comando no container". Os cenários de retomada e de abas reunidas passaram para lá.

### Requirement: Ambiente de execução da sessão
**Reason**: Só existe um ambiente. Não há mais escolha nas configurações nem ambiente diferente por registro.
**Migration**: Sessões e lançamentos do Windows deixam de ser carregados (ver `dados-do-painel`, "Ambiente de execução nos registros"). Os do container continuam abrindo e retomando como antes.
