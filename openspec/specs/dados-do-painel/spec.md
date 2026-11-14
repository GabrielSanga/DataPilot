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
A demanda de identificador `sem-demanda` e título "Sem demanda" SHALL ser criada pelo painel quando for necessária e ainda não existir, e SHALL ser preservada como está quando já existir.

#### Scenario: Arquivo com a demanda implícita
- **WHEN** o arquivo de dados já tem a demanda `sem-demanda` com repositórios e sessões
- **THEN** ela é carregada com esses repositórios e sessões, junto com as demais demandas

#### Scenario: Primeiro repositório cadastrado
- **WHEN** o usuário cria a primeira demanda num arquivo de dados ainda inexistente e vincula a ela o primeiro repositório
- **THEN** o arquivo passa a ter essa demanda com o repositório, sem que a demanda `sem-demanda` seja criada

### Requirement: Cadastro de repositórios
O painel SHALL permitir vincular um repositório a uma demanda pelo nome da pasta, aceitando apenas nomes formados por `[A-Za-z0-9_.-]` e que não sejam compostos só de pontos. Nomes repetidos dentro da mesma demanda, sem diferenciar maiúsculas de minúsculas, SHALL ser recusados. A mesma pasta SHALL poder ser vinculada a demandas diferentes.

#### Scenario: Cadastro válido
- **WHEN** o usuário vincula a pasta `Faturamento` a uma demanda
- **THEN** o repositório passa a constar nessa demanda no arquivo de dados e fica disponível para novas sessões nela

#### Scenario: Nome com caractere fora do permitido
- **WHEN** o usuário tenta vincular um nome com espaço, `\`, `/`, `;` ou qualquer caractere fora do conjunto permitido
- **THEN** o vínculo é recusado com uma mensagem e o arquivo de dados não é alterado

#### Scenario: Nome só de pontos
- **WHEN** o usuário tenta vincular `..`
- **THEN** o vínculo é recusado e o arquivo de dados não é alterado

#### Scenario: Nome repetido
- **WHEN** o usuário tenta vincular `faturamento` a uma demanda que já tem `Faturamento`
- **THEN** o vínculo é recusado e o arquivo de dados não é alterado

#### Scenario: Mesma pasta em outra demanda
- **WHEN** o usuário vincula `Faturamento` a uma demanda e outra demanda já tem `Faturamento`
- **THEN** o vínculo é aceito e cada demanda mantém as próprias sessões desse repositório

### Requirement: Remoção de repositório
O painel SHALL permitir remover um repositório de uma demanda. Se o repositório tiver sessões registradas nessa demanda, a remoção SHALL exigir confirmação informando quantas sessões deixarão de poder ser retomadas. A remoção SHALL NOT afetar o mesmo repositório em outras demandas.

#### Scenario: Repositório sem sessões
- **WHEN** o usuário remove de uma demanda um repositório que não tem sessões registradas nela
- **THEN** ele deixa de constar nessa demanda no arquivo de dados

#### Scenario: Repositório com sessões
- **WHEN** o usuário pede para remover de uma demanda um repositório com sessões registradas
- **THEN** o painel pede confirmação e só remove o repositório e suas sessões dessa demanda se o usuário confirmar

#### Scenario: Mesmo repositório em outra demanda
- **WHEN** o usuário remove `Faturamento` de uma demanda e outra demanda também tem `Faturamento`
- **THEN** o repositório e as sessões da outra demanda permanecem

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
Ao carregar o arquivo de dados, o painel SHALL desconsiderar repositórios cuja `pasta` não siga a regra do cadastro ou repita a de um repositório anterior da mesma demanda, e sessões cujo `sessionId` não seja um GUID canônico ou cujo `agente` não seja `claude` nem `codex`. As entradas desconsideradas SHALL NOT ser usadas em nenhum comando.

#### Scenario: Pasta inválida editada à mão
- **WHEN** o arquivo de dados contém um repositório com `pasta` igual a `..\Windows`
- **THEN** esse repositório não é oferecido para novas sessões nem usado para retomar

#### Scenario: Identificador inválido
- **WHEN** uma sessão do arquivo de dados tem `sessionId` que não é um GUID canônico
- **THEN** essa sessão não pode ser retomada

#### Scenario: Mesma pasta em duas demandas
- **WHEN** o arquivo de dados tem `Faturamento` em duas demandas diferentes
- **THEN** as duas ocorrências são carregadas, cada uma com as próprias sessões

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

### Requirement: Campos da demanda
Cada demanda do arquivo de dados SHALL ter `id`, `titulo`, `arquivada` e `repositorios`, e SHALL poder ter `openspecChange` e `criadaEm`. Demandas criadas pelo painel SHALL ser gravadas com `criadaEm`. Na carga, a ausência de `openspecChange`, `criadaEm` ou `arquivada` SHALL ser aceita, tratando `arquivada` ausente como falso.

#### Scenario: Demanda criada pelo painel
- **WHEN** o usuário cria uma demanda
- **THEN** o arquivo de dados passa a ter um objeto com `id`, `titulo`, `criadaEm`, `arquivada` falso e `repositorios` vazio

#### Scenario: Arquivo gravado por versão anterior
- **WHEN** o arquivo de dados tem a demanda `sem-demanda` sem `criadaEm` nem `openspecChange`
- **THEN** a demanda é carregada normalmente, com seus repositórios e sessões

### Requirement: Sessão única no arquivo
Um mesmo identificador de sessão SHALL constar no máximo uma vez no arquivo de dados. Na carga, se um identificador aparecer mais de uma vez, apenas a primeira ocorrência SHALL ser considerada.

#### Scenario: Identificador repetido em duas demandas
- **WHEN** o arquivo de dados, editado à mão, traz o mesmo `sessionId` em duas demandas
- **THEN** a sessão aparece só na primeira delas

#### Scenario: Mover sessão
- **WHEN** uma sessão é movida para outra demanda
- **THEN** o identificador dela consta apenas na demanda de destino

### Requirement: Validação das demandas ao carregar
Ao carregar o arquivo de dados, o painel SHALL desconsiderar demandas cujo `id` ou `titulo` não sigam as regras de criação ou cujo `id` repita o de uma demanda anterior. Um `openspecChange` inválido SHALL ser tratado como ausente. Demandas desconsideradas SHALL NOT aparecer no painel nem ser usadas em comandos.

#### Scenario: Demanda com identificador inválido
- **WHEN** o arquivo de dados contém uma demanda cujo `id` tem `;`
- **THEN** essa demanda não aparece no painel e nada dela é usado em comandos

#### Scenario: Change do OpenSpec inválida
- **WHEN** uma demanda do arquivo de dados tem `openspecChange` com caracteres fora do permitido
- **THEN** a demanda é carregada sem `openspecChange`
