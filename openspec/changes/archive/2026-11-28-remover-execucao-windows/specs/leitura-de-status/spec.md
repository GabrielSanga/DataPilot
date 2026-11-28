# Spec Delta

## MODIFIED Requirements

### Requirement: Validação do conteúdo
O painel SHALL aceitar um arquivo apenas se ele for um objeto JSON com `agente` igual a `claude` ou `codex`, `session_id` casando com `^[A-Za-z0-9_-]{1,100}$` e igual ao nome do arquivo, `cwd` como texto que começa com `/` (caminho dentro do container), `status` entre `ativa`, `executando`, `aguardando`, `atencao` e `encerrada`, e `atualizado` como data e hora ISO 8601 com fuso. Qualquer outro arquivo SHALL ser ignorado. Campos adicionais SHALL ser desconsiderados.

#### Scenario: Conteúdo que não é JSON
- **WHEN** um arquivo `.json` da pasta contém texto que não é JSON válido
- **THEN** ele não gera sessão e as demais sessões continuam listadas

#### Scenario: Status desconhecido
- **WHEN** um arquivo traz em `status` um valor fora dos cinco previstos
- **THEN** ele não gera sessão

#### Scenario: Identificador diferente do nome do arquivo
- **WHEN** o `session_id` do conteúdo não é igual ao nome do arquivo sem a extensão
- **THEN** ele não gera sessão

#### Scenario: Campo obrigatório ausente
- **WHEN** falta `agente`, `session_id`, `cwd`, `status` ou `atualizado` no arquivo
- **THEN** ele não gera sessão

#### Scenario: Pasta de trabalho do Windows
- **WHEN** um arquivo traz em `cwd` o caminho `C:\Fontes\Faturamento`, gravado pelo antigo hook do Windows
- **THEN** ele não gera sessão, nem em "Sessões" nem em "Mostrar antigas"

#### Scenario: Campo adicional
- **WHEN** um arquivo válido traz também um campo que o painel não conhece
- **THEN** a sessão é listada normalmente

### Requirement: Data de criação do arquivo de status
O painel SHALL conhecer, para cada sessão lida, a data em que ela surgiu na pasta de status pela data de criação do marcador `<session_id>.criado`, obtida do sistema de arquivos, sem alterar nada. Sem o marcador, o painel SHALL NOT conhecer essa data, e a sessão SHALL NOT ser vinculada automaticamente a um lançamento pendente. O painel SHALL NOT ler o conteúdo do marcador, e SHALL desconsiderar um marcador que seja atalho.

#### Scenario: Sessão com vários eventos
- **WHEN** o hook atualizou o arquivo de status de uma sessão várias vezes depois de criar o marcador
- **THEN** a data de criação conhecida pelo painel é a do marcador, e não a do último evento

#### Scenario: Sessão recém-criada
- **WHEN** surgem o marcador e o arquivo de status de uma sessão nova
- **THEN** a data de criação conhecida pelo painel é a do momento em que o marcador surgiu

#### Scenario: Sessão do container com marcador
- **WHEN** a pasta tem o arquivo de status de uma sessão e o marcador `<session_id>.criado`, criado antes da última gravação do arquivo
- **THEN** a data de criação conhecida pelo painel é a do marcador

#### Scenario: Arquivo de status sem marcador
- **WHEN** a pasta tem o arquivo de status válido de uma sessão e não tem o marcador `<session_id>.criado`
- **THEN** a sessão é listada, mas não é vinculada automaticamente a nenhum lançamento pendente

#### Scenario: Marcador que é atalho
- **WHEN** o marcador `<session_id>.criado` é um atalho
- **THEN** o painel age como se o marcador não existisse

#### Scenario: Marcador sem arquivo de status
- **WHEN** a pasta tem um marcador `.criado` sem o arquivo de status correspondente
- **THEN** o marcador não gera sessão
