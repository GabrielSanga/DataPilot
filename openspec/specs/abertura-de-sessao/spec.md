# abertura-de-sessao Specification
## Purpose

Inicia ou retoma uma sessão do Claude Code ou do Codex no repositório certo, rodando como o usuário isolado `ia`, a partir de parâmetros validados recebidos pelo SSH local.

## Requirements

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
Com `-Modo retomar`, o script SHALL retomar a conversa identificada por `-Sessao` no agente indicado. `-Sessao` SHALL ser obrigatório nesse caso. Quando a retomada do Claude terminar com código diferente de 0 em menos de 30 segundos, o script SHALL avisar e iniciar o Claude como sessão nova com o mesmo identificador, e SHALL terminar com o código dessa segunda execução. A retomada do Codex SHALL NOT ter segunda tentativa.

#### Scenario: Retomar sessão do Claude
- **WHEN** o script é chamado com `-Agente claude -Sessao <guid> -Modo retomar`
- **THEN** o Claude Code abre a conversa de identificador `<guid>`, com o histórico anterior

#### Scenario: Retomar sessão do Codex
- **WHEN** o script é chamado com `-Agente codex -Sessao <guid> -Modo retomar`
- **THEN** o Codex abre a conversa de identificador `<guid>`, com o histórico anterior

#### Scenario: Retomada sem identificador
- **WHEN** o script é chamado com `-Modo retomar` e sem `-Sessao`
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

#### Scenario: Sessão do Claude fechada antes da primeira mensagem
- **WHEN** o script é chamado com `-Agente claude -Sessao <guid> -Modo retomar` e o Claude não tem conversa gravada para `<guid>`
- **THEN** o script avisa que a conversa ainda não tem mensagens e o Claude Code inicia uma sessão nova com `<guid>` como identificador

#### Scenario: Retomada do Claude que falha depois de uso
- **WHEN** a retomada do Claude termina com código diferente de 0 depois de mais de 30 segundos
- **THEN** o script não inicia outra sessão e termina com o código do agente

#### Scenario: Retomada do Codex que falha
- **WHEN** a retomada do Codex termina com código diferente de 0
- **THEN** o script não inicia outra sessão e termina com o código do agente

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
O script SHALL funcionar quando invocado como `ssh -t <usuário>@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1` seguido dos parâmetros, e também quando invocado direto como `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1` seguido dos parâmetros, sem exigir aspas em nenhum valor de parâmetro. A validação dos parâmetros SHALL ser a mesma nos dois casos.

#### Scenario: Chamada manual pelo usuário principal
- **WHEN** o usuário principal executa a linha de SSH com `-Agente claude -Repo Faturamento -Sessao <guid> -Modo novo` num terminal
- **THEN** o agente abre nesse terminal, interativo, rodando como `ia`

#### Scenario: Chamada direta, sem SSH
- **WHEN** o usuário principal executa `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1 -Agente codex -Repo Faturamento -Modo novo` num terminal
- **THEN** o agente abre nesse terminal, interativo, rodando como o usuário principal

#### Scenario: Parâmetro inválido na chamada direta
- **WHEN** o script é chamado direto com `-Repo ..\Windows`
- **THEN** o script recusa o parâmetro, não inicia agente e termina com código 2

### Requirement: Scripts protegidos contra alteração pelo usuário ia
`C:\Painel` SHALL permitir que o usuário `ia` leia e execute os scripts, mas não crie, altere nem exclua arquivos nessa pasta. Só o sistema, os administradores e o usuário principal SHALL poder gravar nela, este último para que o painel mantenha os scripts atualizados.

#### Scenario: Usuário ia tenta alterar um script
- **WHEN** um processo do usuário `ia` tenta modificar `C:\Painel\abrir.ps1` ou `C:\Painel\hook.ps1`
- **THEN** a operação é negada

#### Scenario: Usuário ia executa um script
- **WHEN** um agente rodando como `ia` dispara o hook configurado em `C:\Painel\hook.ps1`
- **THEN** o script é executado normalmente

#### Scenario: Usuário principal atualiza um script
- **WHEN** o painel, rodando como o usuário principal e sem elevação, substitui `C:\Painel\abrir.ps1`
- **THEN** a gravação é bem-sucedida

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
