# Spec Delta

## ADDED Requirements

### Requirement: Change do OpenSpec no repositório
Cada repositório de uma demanda SHALL poder ter `openspecChange`, validado com a regra de nome de pasta e até 100 caracteres. Na carga, um `openspecChange` inválido no repositório SHALL ser tratado como ausente, e um repositório sem change própria SHALL usar o `openspecChange` válido da demanda, se o arquivo tiver um. O painel SHALL NOT gravar `openspecChange` na demanda.

#### Scenario: Change gravada no repositório
- **WHEN** o usuário escolhe a change `ajuste-boleto` para o repositório `Faturamento`
- **THEN** o objeto do repositório no arquivo de dados passa a ter `openspecChange` `ajuste-boleto`, e a demanda não ganha esse campo

#### Scenario: Arquivo com a change na demanda
- **WHEN** o arquivo de dados, gravado por versão anterior, tem `openspecChange` na demanda e não nos repositórios
- **THEN** os repositórios da demanda usam essa change

#### Scenario: Repositório com change própria
- **WHEN** a demanda tem `openspecChange` antiga e o repositório tem a própria
- **THEN** o repositório usa a própria

### Requirement: Sessões excluídas
O arquivo de dados SHALL guardar na lista `excluidas` as sessões excluídas pelo usuário, cada uma com `agente`, `sessionId` e `excluidaEm`, com `sessionId` de 1 a 100 caracteres de `[A-Za-z0-9_-]`. A exclusão e a remoção do registro SHALL acontecer na mesma gravação. Sessões excluídas SHALL NOT aparecer no painel nem ser vinculadas automaticamente. Itens inválidos SHALL ser desconsiderados na carga.

#### Scenario: Exclusão gravada
- **WHEN** o usuário exclui uma sessão registrada
- **THEN** a mesma gravação remove o registro e acrescenta o agente, o identificador e a data à lista `excluidas`

#### Scenario: Sessão excluída com lançamento pendente
- **WHEN** uma sessão excluída corresponderia a um lançamento pendente
- **THEN** ela não é vinculada e o lançamento continua pendente

#### Scenario: Arquivo sem a lista
- **WHEN** o arquivo de dados foi gravado por versão anterior e não tem `excluidas`
- **THEN** ele é carregado normalmente

## MODIFIED Requirements

### Requirement: Campos da demanda
Cada demanda do arquivo de dados SHALL ter `id`, `titulo`, `arquivada` e `repositorios`, e SHALL poder ter `criadaEm`. Demandas criadas pelo painel SHALL ser gravadas com `criadaEm` e sem `openspecChange`. Na carga, a ausência de `openspecChange`, `criadaEm` ou `arquivada` SHALL ser aceita, tratando `arquivada` ausente como falso, e um `openspecChange` presente SHALL ser preservado ao gravar.

#### Scenario: Demanda criada pelo painel
- **WHEN** o usuário cria um trabalho
- **THEN** o arquivo de dados passa a ter um objeto com `id` gerado, `titulo`, `criadaEm`, `arquivada` falso e `repositorios` vazio, sem `openspecChange`

#### Scenario: Arquivo gravado por versão anterior
- **WHEN** o arquivo de dados tem a demanda `sem-demanda` sem `criadaEm` nem `openspecChange`
- **THEN** a demanda é carregada normalmente, com seus repositórios e sessões

### Requirement: Validação das demandas ao carregar
Ao carregar o arquivo de dados, o painel SHALL desconsiderar demandas cujo `id` ou `titulo` não sigam as regras ou cujo `id` repita o de uma demanda anterior. Um `openspecChange` inválido, na demanda ou no repositório, SHALL ser tratado como ausente. Demandas desconsideradas SHALL NOT aparecer no painel nem ser usadas em comandos.

#### Scenario: Demanda com identificador inválido
- **WHEN** o arquivo de dados contém uma demanda cujo `id` tem `;`
- **THEN** essa demanda não aparece no painel e nada dela é usado em comandos

#### Scenario: Change do OpenSpec inválida
- **WHEN** um repositório do arquivo de dados tem `openspecChange` com caracteres fora do permitido
- **THEN** o repositório é carregado sem change própria

### Requirement: Dados de status nunca são gravados
O painel SHALL NOT copiar para o arquivo de dados nenhum valor lido de arquivos de status, com duas exceções: no vínculo de uma sessão, acionado pelo usuário, feito por correspondência com um lançamento pendente ou ao retomar uma sessão não registrada, o identificador, validado como GUID canônico, e o agente; e, na exclusão acionada pelo usuário, o identificador e o agente da sessão excluída.

#### Scenario: Sessão desconhecida aparece na pasta de status
- **WHEN** surge um arquivo de status de uma sessão que o painel não abriu, sem lançamento pendente que corresponda a ela, e o usuário não aciona nenhuma ação sobre ela
- **THEN** o arquivo de dados permanece inalterado

#### Scenario: Vínculo acionado pelo usuário
- **WHEN** o usuário vincula uma sessão de "Sessões" a um trabalho
- **THEN** o arquivo de dados passa a ter o identificador e o agente dessa sessão, e nenhum outro valor do arquivo de status

#### Scenario: Vínculo por lançamento
- **WHEN** o painel vincula uma sessão por correspondência com um lançamento pendente
- **THEN** o arquivo de dados passa a ter o identificador e o agente dessa sessão, e nenhum outro valor do arquivo de status

#### Scenario: Exclusão
- **WHEN** o usuário exclui uma sessão
- **THEN** o arquivo de dados passa a ter o identificador e o agente dela em `excluidas`, e nenhum outro valor do arquivo de status

### Requirement: Sessão única no arquivo
Um mesmo identificador de sessão SHALL constar no máximo uma vez entre as sessões registradas do arquivo de dados. Na carga, se um identificador aparecer mais de uma vez, apenas a primeira ocorrência SHALL ser considerada.

#### Scenario: Identificador repetido em duas demandas
- **WHEN** o arquivo de dados, editado à mão, traz o mesmo `sessionId` em duas demandas
- **THEN** a sessão aparece só na primeira delas

#### Scenario: Mover sessão
- **WHEN** uma sessão registrada em "Sessões" é vinculada a um trabalho
- **THEN** o identificador dela passa a constar apenas no trabalho de destino
