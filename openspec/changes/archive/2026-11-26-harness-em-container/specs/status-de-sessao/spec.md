## ADDED Requirements

### Requirement: Marcador de criação no container
A montagem do container não preserva a data de criação de um arquivo substituído. Por isso, o hook do container SHALL criar o marcador vazio `<session_id>.criado` na pasta de status antes de gravar o primeiro status da sessão, e SHALL NOT regravá-lo nos eventos seguintes. Se o marcador não existir e não puder ser criado, o hook SHALL NOT gravar o arquivo de status.

#### Scenario: Primeiro evento no container
- **WHEN** o hook do container grava o primeiro status de uma sessão
- **THEN** a pasta passa a ter o arquivo de status e o marcador `<session_id>.criado`

#### Scenario: Eventos seguintes no container
- **WHEN** o hook do container grava eventos posteriores da mesma sessão
- **THEN** a data de criação do marcador continua sendo a da primeira gravação

### Requirement: Status das sessões no container
O hook do container SHALL gravar na mesma pasta de status das sessões do Windows, que o container vê como `/status`, com o mesmo contrato do arquivo de status. O `cwd` SHALL ser o caminho dentro do container, como `/workspace/Faturamento`.

#### Scenario: Sessão no container
- **WHEN** o hook do container recebe `SessionStart` de uma sessão aberta em `/workspace/Faturamento`
- **THEN** surge `C:\ProgramData\Painel\sessoes\<session_id>.json` com os seis campos e `cwd` igual a `/workspace/Faturamento`

#### Scenario: Associação ao repositório
- **WHEN** o painel lê um arquivo de status com `cwd` igual a `/workspace/Faturamento`
- **THEN** a sessão é associada ao repositório `Faturamento`, como uma sessão do Windows com `cwd` `C:\Fontes\Faturamento`

#### Scenario: Repositório cadastrado por caminho completo
- **WHEN** o painel lê um arquivo de status com `cwd` igual a `/openspec/ERP/Faturamento` e há o repositório `C:\openspec\ERP\Faturamento`
- **THEN** a sessão é associada a esse repositório

## MODIFIED Requirements

### Requirement: Data de criação do arquivo preservada
No Windows, o hook SHALL atualizar o arquivo de status de uma sessão sem alterar a data de criação do arquivo, que SHALL permanecer a da primeira gravação feita para a sessão. No container, a data da primeira gravação SHALL ser guardada pelo marcador de criação.

#### Scenario: Eventos posteriores ao primeiro
- **WHEN** o hook do Windows grava `SessionStart` e, mais tarde, `UserPromptSubmit` e `Stop` da mesma sessão
- **THEN** a data de criação do arquivo de status continua sendo a da primeira gravação

#### Scenario: Primeiro evento da sessão
- **WHEN** o hook grava o primeiro evento de uma sessão que ainda não tem arquivo
- **THEN** a data de criação do arquivo é a do momento dessa gravação

### Requirement: Permissões da pasta de status
Para as sessões no Windows, a instalação SHALL configurar `C:\ProgramData\Painel` de modo que o usuário `ia` possa criar e modificar arquivos de status e o usuário principal possa apenas ler, sem herdar permissões da pasta pai. Para as sessões no container, a pasta de status SHALL ser, junto com a raiz dos fontes e `C:\openspec\ERP`, a única pasta do Windows em que o container pode gravar, e o painel SHALL continuar tratando todo arquivo dela como não confiável.

#### Scenario: Usuário principal tenta escrever
- **WHEN** a instalação do Windows está aplicada e um processo do usuário principal, sem elevação, tenta criar um arquivo na pasta de status
- **THEN** a operação é negada

#### Scenario: Usuário principal lê o status
- **WHEN** um processo do usuário principal lê um arquivo de status gravado pelo `ia` ou pelo container
- **THEN** a leitura é bem-sucedida

#### Scenario: Container tenta gravar fora das montagens
- **WHEN** um processo dentro do container tenta criar um arquivo numa pasta do Windows fora de `/workspace`, `/openspec/ERP` e `/status`
- **THEN** essa pasta não existe dentro do container e nada é gravado no Windows
