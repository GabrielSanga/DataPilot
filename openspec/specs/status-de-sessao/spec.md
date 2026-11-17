# status-de-sessao Specification
## Purpose

Registra o status de cada sessão do Claude Code e do Codex em arquivos que o Painel de Demandas lê, para que o usuário veja o que está executando, aguardando ou pedindo atenção sem interpretar a tela do terminal.

## Requirements

### Requirement: Arquivo de status por sessão
O hook SHALL manter um arquivo por sessão em `C:\ProgramData\Painel\sessoes\<session_id>.json`, contendo um objeto JSON com exatamente os campos `agente`, `session_id`, `cwd`, `status`, `evento` e `atualizado`. `atualizado` SHALL ser a data e hora do evento em ISO 8601 com deslocamento de fuso.

#### Scenario: Primeiro evento da sessão
- **WHEN** o hook recebe `SessionStart` de uma sessão que ainda não tem arquivo
- **THEN** o arquivo `<session_id>.json` é criado com os seis campos preenchidos

#### Scenario: Evento seguinte da mesma sessão
- **WHEN** o hook recebe um novo evento mapeado de uma sessão que já tem arquivo
- **THEN** o mesmo arquivo passa a refletir o novo `status`, `evento` e `atualizado`, e nenhum outro arquivo é criado

### Requirement: Mapa de evento para status
O hook SHALL derivar `status` do nome do evento: `SessionStart` → `ativa`; `UserPromptSubmit` → `executando`; `PostToolUse` → `executando`; `PermissionRequest` → `atencao`; `Stop` → `aguardando`; `SessionEnd` → `encerrada`. Eventos fora do mapa SHALL ser ignorados sem alterar o arquivo.

#### Scenario: Usuário envia um prompt
- **WHEN** o hook recebe `UserPromptSubmit`
- **THEN** o arquivo da sessão registra `status` igual a `executando`

#### Scenario: Agente termina de responder
- **WHEN** o hook recebe `Stop`
- **THEN** o arquivo da sessão registra `status` igual a `aguardando`

#### Scenario: Agente pede permissão
- **WHEN** o hook recebe `PermissionRequest`
- **THEN** o arquivo da sessão registra `status` igual a `atencao`

#### Scenario: Trabalho continua depois de uma permissão aprovada
- **WHEN** a sessão está em `atencao` e o hook recebe `PostToolUse`
- **THEN** o arquivo da sessão volta a registrar `status` igual a `executando`

#### Scenario: Turno longo com várias ferramentas
- **WHEN** o agente executa ferramentas por um período longo sem novo prompt do usuário
- **THEN** `atualizado` avança a cada `PostToolUse`, mantendo `status` igual a `executando`

#### Scenario: Sessão encerrada
- **WHEN** o hook recebe `SessionEnd`
- **THEN** o arquivo da sessão registra `status` igual a `encerrada`

#### Scenario: Evento desconhecido
- **WHEN** o hook recebe um evento que não está no mapa
- **THEN** o arquivo da sessão permanece inalterado

### Requirement: Notificações filtradas por tipo
Para o evento `Notification`, o hook SHALL registrar `atencao` apenas quando o tipo da notificação for `permission_prompt` ou `elicitation_dialog`. Notificações de qualquer outro tipo, inclusive a de ociosidade (`idle_prompt`), SHALL ser ignoradas sem alterar o arquivo.

#### Scenario: Notificação de permissão
- **WHEN** o hook recebe `Notification` do tipo `permission_prompt`
- **THEN** o arquivo da sessão registra `status` igual a `atencao`

#### Scenario: Notificação de ociosidade depois de uma resposta
- **WHEN** a sessão está em `aguardando` e o hook recebe `Notification` do tipo `idle_prompt`
- **THEN** o arquivo da sessão continua com `status` igual a `aguardando`

### Requirement: Eventos fora de ordem não regridem o status
O hook SHALL NOT substituir um arquivo de status cujo `atualizado` seja mais recente que o instante do evento que está processando.

#### Scenario: Evento antigo conclui depois de um mais novo
- **WHEN** o processamento de um `PostToolUse` termina depois que um `Stop` posterior já foi gravado
- **THEN** o arquivo da sessão mantém `status` igual a `aguardando`

### Requirement: Identificação do agente
O campo `agente` SHALL ser `claude` para sessões do Claude Code e `codex` para sessões do Codex, conforme o agente que disparou o hook.

#### Scenario: Sessão do Codex
- **WHEN** um evento mapeado é disparado por uma sessão do Codex
- **THEN** o arquivo da sessão registra `agente` igual a `codex`

### Requirement: Hook silencioso e não bloqueante
O hook SHALL NOT escrever na saída padrão e SHALL terminar com código de saída 0 em qualquer circunstância, inclusive entrada inválida, evento desconhecido ou falha ao gravar.

#### Scenario: Entrada que não é JSON
- **WHEN** o hook recebe na entrada padrão um texto que não é JSON válido
- **THEN** ele termina com código 0, sem saída padrão e sem criar ou alterar arquivos

#### Scenario: Pasta de status indisponível
- **WHEN** a pasta de status não existe ou não aceita escrita
- **THEN** o hook termina com código 0 e sem saída padrão

#### Scenario: Evento de início de sessão
- **WHEN** o hook processa `SessionStart` ou `UserPromptSubmit`
- **THEN** nenhum texto é adicionado ao contexto do agente

### Requirement: Validação do identificador de sessão
O hook SHALL gravar apenas quando `session_id` casar com `^[A-Za-z0-9_-]{1,100}$`. Para qualquer outro valor, o hook SHALL NOT criar nem alterar arquivos.

#### Scenario: Identificador com separador de caminho
- **WHEN** o hook recebe um evento cujo `session_id` contém `\`, `/` ou `.`
- **THEN** nenhum arquivo é criado ou alterado, dentro ou fora da pasta de status

#### Scenario: Identificador ausente
- **WHEN** o hook recebe um evento sem `session_id`
- **THEN** nenhum arquivo é criado ou alterado

### Requirement: Escrita atômica
O hook SHALL atualizar o arquivo de status de forma que um leitor encontre sempre um objeto JSON completo, da versão anterior ou da nova, e nunca um arquivo parcial.

#### Scenario: Leitura durante uma atualização
- **WHEN** o painel lê o arquivo de uma sessão enquanto o hook processa um evento dela
- **THEN** o conteúdo lido é um objeto JSON completo

#### Scenario: Eventos simultâneos da mesma sessão
- **WHEN** dois eventos da mesma sessão são processados ao mesmo tempo
- **THEN** o arquivo resultante é um objeto JSON completo correspondente a um dos dois eventos

### Requirement: Arquivo de status sem conteúdo sensível
O arquivo de status SHALL NOT conter texto de prompts, entradas ou saídas de ferramentas, caminhos de transcrição ou qualquer dado do evento além dos seis campos do contrato.

#### Scenario: Prompt com conteúdo confidencial
- **WHEN** o hook recebe `UserPromptSubmit` com o texto do prompt no evento
- **THEN** o arquivo de status não contém esse texto

### Requirement: Permissões da pasta de status
A instalação SHALL configurar `C:\ProgramData\Painel` de modo que o usuário `ia` possa criar e modificar arquivos de status e o usuário principal possa apenas ler, sem herdar permissões da pasta pai.

#### Scenario: Usuário principal tenta escrever
- **WHEN** um processo do usuário principal, sem elevação, tenta criar um arquivo na pasta de status
- **THEN** a operação é negada

#### Scenario: Usuário principal lê o status
- **WHEN** um processo do usuário principal lê um arquivo de status gravado pelo `ia`
- **THEN** a leitura é bem-sucedida

### Requirement: Data de criação do arquivo preservada
O hook SHALL atualizar o arquivo de status de uma sessão sem alterar a data de criação do arquivo, que SHALL permanecer a da primeira gravação feita para a sessão.

#### Scenario: Eventos posteriores ao primeiro
- **WHEN** o hook grava `SessionStart` e, mais tarde, `UserPromptSubmit` e `Stop` da mesma sessão
- **THEN** a data de criação do arquivo de status continua sendo a da primeira gravação

#### Scenario: Primeiro evento da sessão
- **WHEN** o hook grava o primeiro evento de uma sessão que ainda não tem arquivo
- **THEN** a data de criação do arquivo é a do momento dessa gravação
