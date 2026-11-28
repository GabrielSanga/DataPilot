# Spec Delta

## MODIFIED Requirements

### Requirement: Permissões da pasta de status
A pasta de status SHALL ser, junto com a raiz dos fontes e `C:\openspec\ERP`, a única pasta do Windows em que o container pode gravar, e o painel SHALL continuar tratando todo arquivo dela como não confiável.

#### Scenario: Usuário principal tenta escrever
- **WHEN** um processo do usuário principal grava um arquivo na pasta de status
- **THEN** o painel trata esse arquivo como qualquer outro da pasta: só exibe e compara os valores, sem confiar neles

#### Scenario: Usuário principal lê o status
- **WHEN** um processo do usuário principal lê um arquivo de status gravado pelo container
- **THEN** a leitura é bem-sucedida

#### Scenario: Container tenta gravar fora das montagens
- **WHEN** um processo dentro do container tenta criar um arquivo numa pasta do Windows fora de `/workspace`, `/openspec/ERP` e `/status`
- **THEN** essa pasta não existe dentro do container e nada é gravado no Windows

### Requirement: Status das sessões no container
O hook do container SHALL gravar na pasta de status, que o container vê como `/status`, com o contrato do arquivo de status. O `cwd` SHALL ser o caminho dentro do container, como `/workspace/Faturamento`.

#### Scenario: Sessão no container
- **WHEN** o hook do container recebe `SessionStart` de uma sessão aberta em `/workspace/Faturamento`
- **THEN** surge `C:\ProgramData\Painel\sessoes\<session_id>.json` com os seis campos e `cwd` igual a `/workspace/Faturamento`

#### Scenario: Associação ao repositório
- **WHEN** o painel lê um arquivo de status com `cwd` igual a `/workspace/Faturamento`
- **THEN** a sessão é associada ao repositório `Faturamento`

#### Scenario: Repositório cadastrado por caminho completo
- **WHEN** o painel lê um arquivo de status com `cwd` igual a `/openspec/ERP/Faturamento` e há o repositório `C:\openspec\ERP\Faturamento`
- **THEN** a sessão é associada a esse repositório

## REMOVED Requirements

### Requirement: Data de criação do arquivo preservada
**Reason**: Só o hook do Windows preservava a data de criação do arquivo de status, e esse hook foi removido. A data de início passa a vir só do marcador `<session_id>.criado` (requisito "Marcador de criação no container").
**Migration**: Nenhuma. Arquivos de status antigos do Windows deixam de gerar sessão (ver `leitura-de-status`).
