# dados-do-painel Specification
## Purpose

Guarda, num arquivo que só o usuário principal escreve, os repositórios e as sessões que o painel conhece. É a única fonte de dados que o painel usa para montar comandos.

## Requirements
### Requirement: Arquivo de dados do painel
O painel SHALL manter seus dados em `%APPDATA%\Painel\demandas.json`, em JSON UTF-8, na estrutura `demandas` → `repositorios` → `sessoes` definida no PRD. Se o arquivo não existir, o painel SHALL iniciar sem repositórios e criá-lo na primeira gravação.

#### Scenario: Primeiro uso
- **WHEN** o painel inicia e o arquivo de dados não existe
- **THEN** o painel abre normalmente, sem repositórios cadastrados, e nenhum arquivo é criado até a primeira alteração

#### Scenario: Dados existentes
- **WHEN** o painel inicia e o arquivo de dados contém repositórios e sessões
- **THEN** esses repositórios e sessões ficam disponíveis para abrir e retomar

### Requirement: Demanda implícita
Todo repositório cadastrado SHALL ficar numa única demanda de identificador `sem-demanda` e título "Sem demanda", criada na primeira gravação.

#### Scenario: Primeiro repositório cadastrado
- **WHEN** o usuário cadastra o primeiro repositório
- **THEN** o arquivo de dados passa a ter a demanda `sem-demanda` contendo esse repositório

### Requirement: Cadastro de repositórios
O painel SHALL permitir cadastrar um repositório pelo nome da pasta, aceitando apenas nomes formados por `[A-Za-z0-9_.-]` e que não sejam compostos só de pontos. Nomes repetidos, sem diferenciar maiúsculas de minúsculas, SHALL ser recusados.

#### Scenario: Cadastro válido
- **WHEN** o usuário cadastra a pasta `Faturamento`
- **THEN** o repositório passa a constar no arquivo de dados e fica disponível para novas sessões

#### Scenario: Nome com caractere fora do permitido
- **WHEN** o usuário tenta cadastrar um nome com espaço, `\`, `/`, `;` ou qualquer caractere fora do conjunto permitido
- **THEN** o cadastro é recusado com uma mensagem e o arquivo de dados não é alterado

#### Scenario: Nome só de pontos
- **WHEN** o usuário tenta cadastrar `..`
- **THEN** o cadastro é recusado e o arquivo de dados não é alterado

#### Scenario: Nome repetido
- **WHEN** o usuário tenta cadastrar `faturamento` e já existe `Faturamento`
- **THEN** o cadastro é recusado e o arquivo de dados não é alterado

### Requirement: Remoção de repositório
O painel SHALL permitir remover um repositório cadastrado. Se o repositório tiver sessões registradas, a remoção SHALL exigir confirmação informando quantas sessões deixarão de poder ser retomadas.

#### Scenario: Repositório sem sessões
- **WHEN** o usuário remove um repositório que não tem sessões registradas
- **THEN** ele deixa de constar no arquivo de dados

#### Scenario: Repositório com sessões
- **WHEN** o usuário pede para remover um repositório com sessões registradas
- **THEN** o painel pede confirmação e só remove o repositório e suas sessões se o usuário confirmar

### Requirement: Registro das sessões abertas pelo painel
Ao abrir uma sessão nova do Claude, o painel SHALL registrar no repositório escolhido o agente, o identificador da sessão, o rótulo e a data de criação, antes de iniciar o terminal. Se o terminal não puder ser iniciado, o registro SHALL ser desfeito.

#### Scenario: Sessão nova do Claude
- **WHEN** o usuário abre uma sessão nova do Claude no repositório `Faturamento`
- **THEN** o arquivo de dados passa a ter, nesse repositório, uma sessão com `agente` `claude` e o identificador gerado

#### Scenario: Terminal não inicia
- **WHEN** o painel registra a sessão e a abertura do terminal falha
- **THEN** a sessão registrada é removida do arquivo de dados

#### Scenario: Sessão nova do Codex
- **WHEN** o usuário abre uma sessão nova do Codex
- **THEN** nenhuma sessão é registrada no arquivo de dados

### Requirement: Dados de status nunca são gravados
O painel SHALL NOT copiar para o arquivo de dados nenhum valor lido de arquivos de status.

#### Scenario: Sessão desconhecida aparece na pasta de status
- **WHEN** surge um arquivo de status de uma sessão que o painel não abriu
- **THEN** o arquivo de dados permanece inalterado

### Requirement: Validação ao carregar
Ao carregar o arquivo de dados, o painel SHALL desconsiderar repositórios cuja `pasta` não siga a regra do cadastro e sessões cujo `sessionId` não seja um GUID canônico ou cujo `agente` não seja `claude` nem `codex`. As entradas desconsideradas SHALL NOT ser usadas em nenhum comando.

#### Scenario: Pasta inválida editada à mão
- **WHEN** o arquivo de dados contém um repositório com `pasta` igual a `..\Windows`
- **THEN** esse repositório não é oferecido para novas sessões nem usado para retomar

#### Scenario: Identificador inválido
- **WHEN** uma sessão do arquivo de dados tem `sessionId` que não é um GUID canônico
- **THEN** essa sessão não pode ser retomada

### Requirement: Arquivo ilegível não é sobrescrito
Se o arquivo de dados existir e não puder ser lido como JSON na estrutura esperada, o painel SHALL avisar o usuário, SHALL NOT gravar nele e SHALL desabilitar as ações que dependem dos dados, mantendo a lista de sessões em funcionamento.

#### Scenario: Arquivo corrompido
- **WHEN** o painel inicia e o arquivo de dados contém texto que não é JSON válido
- **THEN** o painel mostra um aviso com o caminho do arquivo, não o altera e desabilita cadastrar, abrir e retomar

### Requirement: Gravação segura
Cada alteração SHALL ser gravada de modo que o arquivo de dados contenha sempre a versão anterior ou a nova, completas. Campos e entradas que o painel não reconhece SHALL ser preservados na gravação.

#### Scenario: Falha durante a gravação
- **WHEN** a gravação de uma alteração é interrompida
- **THEN** o arquivo de dados continua com o conteúdo anterior, completo

#### Scenario: Campo desconhecido
- **WHEN** o arquivo de dados tem um campo que esta versão do painel não usa e o usuário cadastra um repositório
- **THEN** o campo desconhecido continua no arquivo depois da gravação
