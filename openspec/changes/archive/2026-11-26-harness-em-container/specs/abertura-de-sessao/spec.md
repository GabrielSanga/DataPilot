## ADDED Requirements

### Requirement: Script de abertura no container
Dentro do container, o script `/opt/painel/abrir.sh` SHALL seguir as mesmas regras do `abrir.ps1` para nova sessão, retomada (inclusive a segunda tentativa do Claude em menos de 30 segundos), validação dos parâmetros e pasta de trabalho dentro da raiz dos fontes. Os parâmetros SHALL se chamar `--agente`, `--repo` ou `--caminho` (um dos dois), `--sessao` e `--modo`. A raiz dos fontes SHALL ser `/workspace` e o segundo local `/openspec/ERP`, ambos fixos no script. `--repo` SHALL ser uma pasta direta de `/workspace`. `--caminho` SHALL ser o caminho no container, em Base64 (UTF-8), de uma pasta que seja um desses dois locais ou esteja dentro deles, sem trechos `.` e `..` e sem passar por atalho. O agente SHALL rodar com o usuário sem privilégios do container.

#### Scenario: Nova sessão do Claude no container
- **WHEN** o script é chamado com `--agente claude --repo Faturamento --sessao <guid> --modo novo` e `/workspace/Faturamento` existe
- **THEN** o Claude Code inicia em modo interativo em `/workspace/Faturamento`, com `<guid>` como identificador da sessão

#### Scenario: Retomada do Codex no container
- **WHEN** o script é chamado com `--agente codex --repo Faturamento --sessao <guid> --modo retomar`
- **THEN** o Codex abre a conversa de identificador `<guid>`, com o histórico anterior

#### Scenario: Usuário que executa o agente
- **WHEN** o script inicia um agente
- **THEN** o agente roda com o usuário sem privilégios do container, e não como `root`

#### Scenario: Subpasta pelo caminho
- **WHEN** o script é chamado com `--caminho` igual ao Base64 de `/openspec/ERP/Faturamento` e a pasta existe
- **THEN** o agente inicia em `/openspec/ERP/Faturamento`

#### Scenario: Caminho fora dos locais
- **WHEN** o script é chamado com `--caminho` igual ao Base64 de `/home/node`, ou de um caminho com `..`
- **THEN** nenhum agente é iniciado e o script termina com código 2

### Requirement: Chamada do script pelo docker exec
O `abrir.sh` SHALL funcionar chamado por `docker exec --interactive --tty <container> /opt/painel/abrir.sh` seguido dos parâmetros, sem exigir aspas em nenhum valor. Os códigos de saída próprios SHALL ser os mesmos do `abrir.ps1`: 2 para parâmetro inválido, 3 para repositório não encontrado e 4 para agente não encontrado.

#### Scenario: Chamada manual pelo usuário principal
- **WHEN** o usuário principal executa `docker exec --interactive --tty datapilot-agente /opt/painel/abrir.sh --agente codex --repo Faturamento --modo novo` num terminal
- **THEN** o Codex abre nesse terminal, interativo, rodando dentro do container

#### Scenario: Repositório apontando para a pasta pai
- **WHEN** o script é chamado com `--repo ..`
- **THEN** nenhum agente é iniciado e o script termina com código 2

#### Scenario: Repositório inexistente
- **WHEN** o script é chamado com um `--repo` válido cuja pasta não existe em `/workspace`
- **THEN** o script informa que o repositório não foi encontrado, não inicia agente e termina com código 3
