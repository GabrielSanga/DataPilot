## MODIFIED Requirements

### Requirement: Data de criação do arquivo de status
O painel SHALL conhecer, para cada sessão lida, a data em que ela surgiu na pasta de status, obtida do sistema de arquivos, sem alterar nada: a data de criação do marcador `<session_id>.criado`, quando ele existir na pasta, e a do próprio arquivo de status, quando não existir. O painel SHALL NOT ler o conteúdo do marcador, e SHALL desconsiderar um marcador que seja atalho.

#### Scenario: Sessão com vários eventos
- **WHEN** o hook atualizou o arquivo de status de uma sessão várias vezes depois de criá-lo
- **THEN** a data de criação conhecida pelo painel é a da primeira gravação, e não a do último evento

#### Scenario: Sessão recém-criada
- **WHEN** surge o arquivo de status de uma sessão nova
- **THEN** a data de criação conhecida pelo painel é a do momento em que o arquivo surgiu

#### Scenario: Sessão do container com marcador
- **WHEN** a pasta tem o arquivo de status de uma sessão e o marcador `<session_id>.criado`, criado antes da última gravação do arquivo
- **THEN** a data de criação conhecida pelo painel é a do marcador

#### Scenario: Marcador sem arquivo de status
- **WHEN** a pasta tem um marcador `.criado` sem o arquivo de status correspondente
- **THEN** o marcador não gera sessão
