# Spec Delta

## MODIFIED Requirements

### Requirement: Nova sessão do Claude com identificador definido
Com `--agente claude --modo novo`, o script SHALL iniciar uma sessão interativa do Claude Code cujo identificador é o valor de `--sessao`. `--sessao` SHALL ser obrigatório nesse caso.

#### Scenario: Nova sessão com identificador gerado por quem chama
- **WHEN** o script é chamado com `--agente claude --repo Faturamento --sessao <guid> --modo novo`
- **THEN** o Claude Code inicia em modo interativo e a sessão criada tem `<guid>` como identificador

#### Scenario: Nova sessão do Claude sem identificador
- **WHEN** o script é chamado com `--agente claude --modo novo` e sem `--sessao`
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

### Requirement: Nova sessão do Codex
Com `--agente codex --modo novo`, o script SHALL iniciar uma sessão interativa do Codex sem exigir `--sessao`.

#### Scenario: Nova sessão do Codex
- **WHEN** o script é chamado com `--agente codex --repo Faturamento --modo novo`
- **THEN** o Codex inicia em modo interativo no repositório indicado

### Requirement: Retomada de sessão
Com `--modo retomar`, o script SHALL retomar a conversa identificada por `--sessao` no agente indicado. `--sessao` SHALL ser obrigatório nesse caso. Quando a retomada do Claude terminar com código diferente de 0 em menos de 30 segundos, o script SHALL avisar e iniciar o Claude como sessão nova com o mesmo identificador, e SHALL terminar com o código dessa segunda execução. A retomada do Codex SHALL NOT ter segunda tentativa.

#### Scenario: Retomar sessão do Claude
- **WHEN** o script é chamado com `--agente claude --sessao <guid> --modo retomar`
- **THEN** o Claude Code abre a conversa de identificador `<guid>`, com o histórico anterior

#### Scenario: Retomar sessão do Codex
- **WHEN** o script é chamado com `--agente codex --sessao <guid> --modo retomar`
- **THEN** o Codex abre a conversa de identificador `<guid>`, com o histórico anterior

#### Scenario: Retomada sem identificador
- **WHEN** o script é chamado com `--modo retomar` e sem `--sessao`
- **THEN** nenhum agente é iniciado e o script termina com código diferente de 0

#### Scenario: Sessão do Claude fechada antes da primeira mensagem
- **WHEN** o script é chamado com `--agente claude --sessao <guid> --modo retomar` e o Claude não tem conversa gravada para `<guid>`
- **THEN** o script avisa que a conversa ainda não tem mensagens e o Claude Code inicia uma sessão nova com `<guid>` como identificador

#### Scenario: Retomada do Claude que falha depois de uso
- **WHEN** a retomada do Claude termina com código diferente de 0 depois de mais de 30 segundos
- **THEN** o script não inicia outra sessão e termina com o código do agente

#### Scenario: Retomada do Codex que falha
- **WHEN** a retomada do Codex termina com código diferente de 0
- **THEN** o script não inicia outra sessão e termina com o código do agente

### Requirement: Validação dos parâmetros
O script SHALL aceitar apenas: `--agente` igual a `claude` ou `codex`; `--modo` igual a `novo` ou `retomar`; `--repo` formado só por `[A-Za-z0-9_.-]` e não composto apenas de pontos; `--sessao` na forma canônica de GUID (8-4-4-4-12 dígitos hexadecimais). Para qualquer outro valor, o script SHALL NOT iniciar agente nem mudar de pasta, e SHALL terminar com código 2.

#### Scenario: Repositório apontando para a pasta pai
- **WHEN** o script é chamado com `--repo ..`
- **THEN** nenhum agente é iniciado e o script termina com código 2

#### Scenario: Repositório com caractere de comando
- **WHEN** o script é chamado com `--repo` contendo espaço, `;`, `&`, `|`, `\` ou `/`
- **THEN** nenhum agente é iniciado e o script termina com código 2

#### Scenario: Identificador de sessão malformado
- **WHEN** o script é chamado com `--sessao` de 36 caracteres que não formam um GUID canônico
- **THEN** nenhum agente é iniciado e o script termina com código 2

#### Scenario: Agente desconhecido
- **WHEN** o script é chamado com `--agente` diferente de `claude` e `codex`
- **THEN** nenhum agente é iniciado e o script termina com código 2

### Requirement: Pasta de trabalho dentro da raiz dos fontes
Com `--repo`, o script SHALL iniciar o agente com a pasta de trabalho `/workspace/<repo>`, em que `/workspace` é um valor fixo definido no próprio script e nunca recebido por parâmetro. A pasta resultante SHALL ser filha direta de `/workspace`.

#### Scenario: Repositório existente
- **WHEN** o script é chamado com `--repo Faturamento` e a pasta `/workspace/Faturamento` existe
- **THEN** o agente inicia com `/workspace/Faturamento` como pasta de trabalho

#### Scenario: Repositório inexistente
- **WHEN** o script é chamado com um `--repo` válido cuja pasta não existe em `/workspace`
- **THEN** o script informa que o repositório não foi encontrado, não inicia agente e termina com código 3

### Requirement: Script de abertura no container
O script de abertura SHALL ser o `/opt/painel/abrir.sh`, dentro do container. Os parâmetros SHALL se chamar `--agente`, `--repo` ou `--caminho` (um dos dois), `--sessao` e `--modo`. O segundo local SHALL ser `/openspec/ERP`, fixo no script. `--caminho` SHALL ser o caminho no container, em Base64 (UTF-8), de uma pasta que seja `/workspace` ou `/openspec/ERP` ou esteja dentro deles, sem trechos `.` e `..` e sem passar por atalho. O agente SHALL rodar com o usuário sem privilégios do container.

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
O `abrir.sh` SHALL funcionar chamado por `docker exec --interactive --tty <container> /opt/painel/abrir.sh` seguido dos parâmetros, sem exigir aspas em nenhum valor. Os códigos de saída próprios SHALL ser 2 para parâmetro inválido, 3 para repositório não encontrado e 4 para agente não encontrado.

#### Scenario: Chamada manual pelo usuário principal
- **WHEN** o usuário principal executa `docker exec --interactive --tty datapilot-agente /opt/painel/abrir.sh --agente codex --repo Faturamento --modo novo` num terminal
- **THEN** o Codex abre nesse terminal, interativo, rodando dentro do container

#### Scenario: Repositório apontando para a pasta pai
- **WHEN** o script é chamado com `--repo ..`
- **THEN** nenhum agente é iniciado e o script termina com código 2

#### Scenario: Repositório inexistente
- **WHEN** o script é chamado com um `--repo` válido cuja pasta não existe em `/workspace`
- **THEN** o script informa que o repositório não foi encontrado, não inicia agente e termina com código 3

#### Scenario: Agente ausente
- **WHEN** o script é chamado com parâmetros válidos e o executável do agente não existe no container
- **THEN** nenhum agente é iniciado e o script termina com código 4

## REMOVED Requirements

### Requirement: Chamada pelo SSH local em uma única linha
**Reason**: O modo Windows foi removido. O painel não chama mais o `abrir.ps1`, nem pelo SSH nem direto.
**Migration**: A abertura passa a ser só pelo `docker exec` no container (requisito "Chamada do script pelo docker exec"). O OpenSSH Server e o usuário `ia` podem ser desinstalados à mão.

### Requirement: Scripts protegidos contra alteração pelo usuário ia
**Reason**: O usuário `ia` e `C:\Painel` deixam de ser usados. A proteção dos scripts do container contra o agente está em `ambiente-container` ("Scripts do painel protegidos dentro da imagem").
**Migration**: Nenhuma.
