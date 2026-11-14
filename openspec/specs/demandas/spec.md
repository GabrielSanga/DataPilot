# demandas Specification
## Purpose

Organiza o trabalho do painel por demanda: cada demanda reúne os repositórios e as sessões de um mesmo trabalho, mesmo quando ele envolve vários módulos, e pode ser arquivada quando termina.

## Requirements
### Requirement: Criar demanda
O painel SHALL permitir criar uma demanda informando `id`, `titulo` e, opcionalmente, `openspecChange`. O `id` SHALL ter de 1 a 30 caracteres de `[A-Za-z0-9_.-]`, não ser formado só por pontos e ser único sem diferenciar maiúsculas de minúsculas. O `titulo` SHALL ter de 1 a 80 caracteres, sem caracteres de controle. O `openspecChange`, quando informado, SHALL seguir a regra de caracteres do `id`.

#### Scenario: Demanda válida
- **WHEN** o usuário cria a demanda de `id` `CHG-142` e título "Ajuste NF-e"
- **THEN** a demanda passa a constar no arquivo de dados, com a data de criação, e aparece na lista de demandas

#### Scenario: Identificador repetido
- **WHEN** o usuário tenta criar uma demanda com `id` `chg-142` e já existe `CHG-142`
- **THEN** a criação é recusada com uma mensagem e o arquivo de dados não é alterado

#### Scenario: Identificador com caractere fora do permitido
- **WHEN** o usuário informa um `id` com espaço, `;` ou outro caractere fora do conjunto permitido
- **THEN** a criação é recusada e o arquivo de dados não é alterado

#### Scenario: Título vazio
- **WHEN** o usuário tenta criar uma demanda sem título
- **THEN** a criação é recusada e o arquivo de dados não é alterado

### Requirement: Editar demanda
O painel SHALL permitir alterar `id`, `titulo` e `openspecChange` de uma demanda, aplicando as mesmas regras da criação. Repositórios e sessões da demanda SHALL ser mantidos na edição.

#### Scenario: Alterar o título
- **WHEN** o usuário altera o título de uma demanda
- **THEN** o novo título aparece na lista e no arquivo de dados, e os repositórios e as sessões da demanda continuam os mesmos

#### Scenario: Alterar o identificador para um já usado
- **WHEN** o usuário altera o `id` de uma demanda para o de outra demanda existente
- **THEN** a alteração é recusada e o arquivo de dados não é alterado

### Requirement: Arquivar e desarquivar
O painel SHALL permitir arquivar e desarquivar uma demanda. Demandas arquivadas SHALL ficar ocultas da lista, a menos que o usuário ative a opção de mostrar arquivadas. Ao arquivar uma demanda que tenha sessão exibida em status diferente de "encerrada", "inativa" e "sem status", o painel SHALL avisar e arquivar somente se o usuário confirmar.

#### Scenario: Arquivar demanda concluída
- **WHEN** o usuário arquiva uma demanda cujas sessões estão encerradas
- **THEN** a demanda deixa de aparecer na lista e fica marcada como arquivada no arquivo de dados

#### Scenario: Ver arquivadas
- **WHEN** o usuário ativa a opção de mostrar arquivadas
- **THEN** as demandas arquivadas aparecem na lista, identificadas como arquivadas

#### Scenario: Desarquivar
- **WHEN** o usuário desarquiva uma demanda
- **THEN** ela volta a aparecer na lista sem a opção de mostrar arquivadas

#### Scenario: Arquivar com sessão em andamento
- **WHEN** o usuário arquiva uma demanda que tem uma sessão exibida como "executando"
- **THEN** o painel avisa e só arquiva se o usuário confirmar

### Requirement: Demanda fixa "Sem demanda"
A demanda de `id` `sem-demanda` SHALL aparecer na lista sempre que tiver repositórios, e SHALL NOT poder ser editada nem arquivada.

#### Scenario: Sessões abertas antes das demandas
- **WHEN** o arquivo de dados tem repositórios e sessões na demanda `sem-demanda`
- **THEN** ela aparece na lista de demandas com o título "Sem demanda"

#### Scenario: Tentativa de arquivar
- **WHEN** o usuário seleciona a demanda "Sem demanda"
- **THEN** as ações de editar e arquivar não são oferecidas

### Requirement: Vincular repositório a uma demanda
O painel SHALL permitir vincular um repositório à demanda selecionada, informando o nome da pasta, e SHALL sugerir as pastas já usadas em outras demandas.

#### Scenario: Repositório já usado em outra demanda
- **WHEN** o usuário vincula um repositório a uma demanda e a pasta `Faturamento` já é usada em outra
- **THEN** `Faturamento` aparece entre as sugestões e pode ser vinculado também à demanda selecionada

#### Scenario: Demanda com dois repositórios
- **WHEN** o usuário vincula `Faturamento` e `Estoque` à mesma demanda
- **THEN** a demanda mostra um card para cada repositório

### Requirement: Mover sessão para outra demanda
O painel SHALL permitir mover uma sessão registrada para outra demanda. A sessão SHALL passar ao repositório de mesma pasta na demanda de destino, que SHALL ser vinculado a ela se ainda não estiver. A sessão SHALL continuar podendo ser retomada.

#### Scenario: Mover de "Sem demanda" para uma demanda
- **WHEN** o usuário move uma sessão registrada em `Faturamento`, na demanda "Sem demanda", para a demanda `CHG-142`
- **THEN** a sessão deixa de constar em "Sem demanda" e passa a constar em `Faturamento` dentro de `CHG-142`

#### Scenario: Destino ainda sem o repositório
- **WHEN** a demanda de destino não tem o repositório `Faturamento`
- **THEN** o repositório é vinculado à demanda de destino e a sessão fica nele

#### Scenario: Retomar depois de mover
- **WHEN** o usuário aciona "Retomar" numa sessão que foi movida
- **THEN** a conversa certa abre, no mesmo repositório de antes

### Requirement: Remover o registro de uma sessão
O painel SHALL permitir remover uma sessão registrada, após confirmação que informe que a conversa não é apagada e que a sessão deixará de poder ser retomada pelo painel.

#### Scenario: Remoção confirmada
- **WHEN** o usuário confirma a remoção de uma sessão registrada
- **THEN** a sessão deixa de constar no arquivo de dados e nenhum arquivo do agente ou da pasta de status é alterado

#### Scenario: Remoção cancelada
- **WHEN** o usuário cancela a confirmação
- **THEN** o arquivo de dados não é alterado
