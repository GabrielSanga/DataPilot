# Spec Delta

## Purpose

Inicia ou retoma uma sessão do Claude Code ou do Codex no repositório certo, rodando como o usuário isolado `ia`, a partir de parâmetros validados recebidos pelo SSH local.

## ADDED Requirements

### Requirement: Nova sessão do Claude com identificador definido
Com `-Agente claude -Modo novo`, o script SHALL iniciar uma sessão interativa do Claude Code cujo identificador é o valor de `-Sessao`. `-Sessao` SHALL ser obrigatório nesse caso.

#### Scenario: Nova sessão com identificador gerado por quem chama
- **WHEN** o script é chamado com `-Agente claude -Repo Faturamento -Sessao <guid> -Modo novo`
- **THEN** o Claude Code inicia em modo interativo e a sessão criada tem `<guid>` como identificador

#### Scenario: Nova sessão do Claude sem identificador
- **WHEN** o script é chamado com `-Agente claude -Modo novo` e sem `-Sessao`
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

### Requirement: Nova sessão do Codex
Com `-Agente codex -Modo novo`, o script SHALL iniciar uma sessão interativa do Codex sem exigir `-Sessao`.

#### Scenario: Nova sessão do Codex
- **WHEN** o script é chamado com `-Agente codex -Repo Faturamento -Modo novo`
- **THEN** o Codex inicia em modo interativo no repositório indicado

### Requirement: Retomada de sessão
Com `-Modo retomar`, o script SHALL retomar a conversa identificada por `-Sessao` no agente indicado. `-Sessao` SHALL ser obrigatório nesse caso.

#### Scenario: Retomar sessão do Claude
- **WHEN** o script é chamado com `-Agente claude -Sessao <guid> -Modo retomar`
- **THEN** o Claude Code abre a conversa de identificador `<guid>`, com o histórico anterior

#### Scenario: Retomar sessão do Codex
- **WHEN** o script é chamado com `-Agente codex -Sessao <guid> -Modo retomar`
- **THEN** o Codex abre a conversa de identificador `<guid>`, com o histórico anterior

#### Scenario: Retomada sem identificador
- **WHEN** o script é chamado com `-Modo retomar` e sem `-Sessao`
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

### Requirement: Validação dos parâmetros
O script SHALL aceitar apenas: `-Agente` igual a `claude` ou `codex`; `-Modo` igual a `novo` ou `retomar`; `-Repo` formado só por `[A-Za-z0-9_.-]` e não composto apenas de pontos; `-Sessao` na forma canônica de GUID (8-4-4-4-12 dígitos hexadecimais). Para qualquer outro valor, o script SHALL NOT iniciar agente nem mudar de pasta, e SHALL terminar com código diferente de 0.

#### Scenario: Repositório apontando para a pasta pai
- **WHEN** o script é chamado com `-Repo ..`
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

#### Scenario: Repositório com caractere de comando
- **WHEN** o script é chamado com `-Repo` contendo espaço, `;`, `&`, `|`, `\` ou `/`
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

#### Scenario: Identificador de sessão malformado
- **WHEN** o script é chamado com `-Sessao` de 36 caracteres que não formam um GUID canônico
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

#### Scenario: Agente desconhecido
- **WHEN** o script é chamado com `-Agente` diferente de `claude` e `codex`
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

### Requirement: Pasta de trabalho dentro da raiz dos fontes
O script SHALL iniciar o agente com a pasta de trabalho `<raiz>\<Repo>`, em que `<raiz>` é um valor fixo definido no próprio script e nunca recebido por parâmetro. A pasta resultante SHALL ser filha direta de `<raiz>`.

#### Scenario: Repositório existente
- **WHEN** o script é chamado com `-Repo Faturamento` e a pasta `<raiz>\Faturamento` existe
- **THEN** o agente inicia com `<raiz>\Faturamento` como pasta de trabalho

#### Scenario: Repositório inexistente
- **WHEN** o script é chamado com um `-Repo` válido cuja pasta não existe em `<raiz>`
- **THEN** o script informa que o repositório não foi encontrado, não inicia agente e termina com código diferente de 0

### Requirement: Chamada pelo SSH local em uma única linha
O script SHALL funcionar quando invocado como `ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1` seguido dos parâmetros, sem exigir aspas em nenhum valor de parâmetro.

#### Scenario: Chamada manual pelo usuário principal
- **WHEN** o usuário principal executa a linha de SSH com `-Agente claude -Repo Faturamento -Sessao <guid> -Modo novo` num terminal
- **THEN** o agente abre nesse terminal, interativo, rodando como `ia`

### Requirement: Scripts protegidos contra alteração pelo usuário ia
A instalação SHALL configurar `C:\Painel` de modo que o usuário `ia` possa ler e executar os scripts, mas não criar, alterar nem excluir arquivos nessa pasta.

#### Scenario: Usuário ia tenta alterar um script
- **WHEN** um processo do usuário `ia` tenta modificar `C:\Painel\abrir.ps1` ou `C:\Painel\hook.ps1`
- **THEN** a operação é negada

#### Scenario: Usuário ia executa um script
- **WHEN** um agente rodando como `ia` dispara o hook configurado em `C:\Painel\hook.ps1`
- **THEN** o script é executado normalmente
