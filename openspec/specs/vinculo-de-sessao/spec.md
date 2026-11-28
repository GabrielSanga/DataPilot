# vinculo-de-sessao Specification
## Purpose

Permite associar a uma demanda uma sessão que o painel não abriu, como as do Codex e as criadas por `/clear`, para que ela apareça na demanda e possa ser retomada, deixando passar do arquivo de status para os dados do painel apenas o mínimo necessário e sempre por decisão do usuário.

## Requirements

### Requirement: Vincular sessão não vinculada
O painel SHALL permitir vincular uma sessão de "Sessões" a uma demanda não arquivada, em um repositório dessa demanda ou em um repositório novo escolhido pelo usuário, com um rótulo opcional. Uma sessão já registrada em `sem-demanda` SHALL ser movida para a demanda escolhida. Depois de vinculada, a sessão SHALL constar no arquivo de dados, aparecer no card do repositório com seu status e poder ser retomada.

#### Scenario: Sessão do Codex
- **WHEN** o usuário vincula uma sessão do Codex de "Sessões" ao repositório `Faturamento` de um trabalho
- **THEN** a sessão passa a aparecer no card `Faturamento` desse trabalho, com o status atual, e oferece "Retomar"

#### Scenario: Sessão criada por /clear
- **WHEN** o usuário vincula a sessão que surgiu depois de um `/clear` numa sessão do Claude
- **THEN** a sessão nova passa a aparecer no trabalho escolhido e pode ser retomada

#### Scenario: Retomar sessão vinculada
- **WHEN** o usuário aciona "Retomar" numa sessão do Codex vinculada
- **THEN** abre uma aba com a conversa anterior do Codex, no repositório escolhido no vínculo

#### Scenario: Nenhuma demanda disponível
- **WHEN** o usuário aciona "Vincular a um trabalho" e não existe trabalho não arquivado
- **THEN** o painel orienta a criar um trabalho e não grava nada

### Requirement: Somente identificador e agente cruzam
No vínculo, manual ou por lançamento, os únicos valores do arquivo de status gravados no arquivo de dados SHALL ser o identificador da sessão e o agente. A demanda, o repositório e o rótulo SHALL vir da escolha do usuário, feita no diálogo de vínculo ou ao abrir a sessão pelo card. A pasta de trabalho, o status, o evento e as datas do arquivo de status SHALL NOT ser gravados.

#### Scenario: Pasta de trabalho diferente do repositório escolhido
- **WHEN** o usuário vincula ao repositório `Faturamento` uma sessão cujo arquivo de status traz outra pasta de trabalho
- **THEN** a sessão é gravada em `Faturamento`, e a pasta de trabalho do arquivo de status não consta no arquivo de dados

#### Scenario: Conteúdo gravado
- **WHEN** uma sessão é vinculada
- **THEN** o registro dela no arquivo de dados tem o agente, o identificador, o rótulo quando informado e as datas, e nenhum outro valor vindo do arquivo de status

#### Scenario: Conteúdo gravado no vínculo por lançamento
- **WHEN** uma sessão é vinculada por um lançamento pendente
- **THEN** o registro dela tem o agente, o identificador, o rótulo do lançamento e datas geradas pelo painel, e não tem a pasta de trabalho nem a data de criação do arquivo de status

### Requirement: Somente GUID canônico pode ser vinculado
O painel SHALL permitir vincular apenas sessões cujo identificador seja um GUID canônico (8-4-4-4-12 dígitos hexadecimais). Para as demais, a ação SHALL ficar indisponível, com a indicação do motivo.

#### Scenario: Identificador fora do formato
- **WHEN** uma sessão de "Não vinculadas" tem um identificador que não é um GUID canônico
- **THEN** a ação de vincular não está disponível para ela e o painel indica que o identificador não pode ser usado para retomar

### Requirement: Vínculo apenas por ação explícita
O painel SHALL NOT vincular nenhuma sessão que não tenha sido objeto de uma ação de vínculo do usuário nem corresponda a um lançamento pendente, registrado quando o usuário abriu uma sessão nova pelo painel.

#### Scenario: Codex aberto pelo painel
- **WHEN** o usuário abre uma sessão nova do Codex pelo card de um repositório e o arquivo de status dela surge depois disso
- **THEN** a sessão é registrada nesse repositório dessa demanda, sem outra ação do usuário

#### Scenario: Sessão não vinculada na pasta de um repositório da demanda
- **WHEN** existe uma sessão não vinculada cuja pasta de trabalho tem o mesmo nome de um repositório de uma demanda, e não há lançamento pendente nesse repositório
- **THEN** ela aparece somente em "Não vinculadas", e o card do repositório mostra apenas sessões registradas

### Requirement: Aviso de pasta divergente
Ao vincular pelo diálogo, se o último segmento da pasta de trabalho informada pela sessão não coincidir com o repositório escolhido, o painel SHALL avisar que a retomada pode não encontrar a conversa e SHALL vincular somente se o usuário confirmar.

#### Scenario: Repositório diferente da pasta informada
- **WHEN** o usuário escolhe o repositório `Estoque` para uma sessão cuja pasta de trabalho termina em `Faturamento`
- **THEN** o painel avisa e só grava o vínculo se o usuário confirmar

#### Scenario: Repositório igual à pasta informada
- **WHEN** o usuário escolhe o repositório `Faturamento` para uma sessão cuja pasta de trabalho termina em `Faturamento`
- **THEN** o vínculo é gravado sem aviso

### Requirement: Rastro do vínculo
Uma sessão vinculada, manualmente ou por lançamento, SHALL ser gravada com o campo `vinculadaEm`, com a data e hora do vínculo. Sessões registradas pelo painel no momento da abertura SHALL NOT ter esse campo.

#### Scenario: Sessão vinculada
- **WHEN** o usuário vincula uma sessão
- **THEN** o registro dela no arquivo de dados tem `vinculadaEm`

#### Scenario: Sessão aberta pelo painel
- **WHEN** o usuário abre uma sessão nova do Claude pelo painel
- **THEN** o registro dela no arquivo de dados não tem `vinculadaEm`

#### Scenario: Sessão do Codex vinculada por lançamento
- **WHEN** o painel vincula uma sessão do Codex por um lançamento pendente
- **THEN** o registro dela no arquivo de dados tem `vinculadaEm`

### Requirement: Sessão já registrada não é vinculada de novo
O painel SHALL recusar o vínculo de uma sessão cujo identificador já conste no arquivo de dados.

#### Scenario: Vínculo repetido
- **WHEN** o usuário tenta vincular uma sessão que já foi registrada por outra janela do painel
- **THEN** o vínculo é recusado com uma mensagem e o arquivo de dados não é alterado

### Requirement: Vínculo automático por lançamento pendente
O painel SHALL registrar, sem outra ação do usuário, uma sessão não registrada que corresponda a um lançamento pendente, na demanda e no repositório desse lançamento e com o rótulo dele. Há correspondência quando o agente é o do lançamento, o identificador é um GUID canônico, o último segmento da pasta de trabalho é o nome do repositório e o arquivo de status foi criado depois do lançamento. Cada lançamento SHALL vincular uma única sessão.

#### Scenario: Codex aberto pelo card
- **WHEN** o usuário abre uma sessão nova do Codex no card `Faturamento` de `CHG-142`, com o rótulo `ajuste`, e envia a primeira mensagem
- **THEN** a sessão passa a aparecer no card `Faturamento` de `CHG-142`, com o rótulo `ajuste`, o status e "Retomar", sem passar por "Não vinculadas"

#### Scenario: Antes da primeira mensagem
- **WHEN** o usuário abre uma sessão nova do Codex pelo card e ainda não enviou nenhuma mensagem
- **THEN** nenhuma sessão nova aparece no card e o lançamento continua pendente

#### Scenario: Sessão que já existia antes do clique
- **WHEN** existe uma sessão do Codex não vinculada na pasta `Faturamento`, criada antes do lançamento, e ela recebe uma mensagem depois dele
- **THEN** a sessão permanece em "Não vinculadas" e o lançamento continua pendente

#### Scenario: Agente diferente do lançamento
- **WHEN** surge uma sessão do Claude não registrada na pasta `Faturamento` e o único lançamento pendente nesse repositório é do Codex
- **THEN** a sessão não é registrada e aparece em "Não vinculadas"

#### Scenario: Pasta diferente do repositório
- **WHEN** surge uma sessão do Codex cuja pasta de trabalho termina em `Estoque` e só há lançamento pendente no repositório `Faturamento`
- **THEN** a sessão não é registrada e aparece em "Não vinculadas"

#### Scenario: Segunda sessão no mesmo terminal
- **WHEN** a sessão de um lançamento já foi vinculada e surge outra sessão do Codex na mesma pasta, sem novo lançamento
- **THEN** a segunda sessão aparece em "Não vinculadas"

### Requirement: Limites do vínculo automático
O painel SHALL NOT vincular automaticamente por um lançamento registrado há mais de 24 horas, nem quando a demanda do lançamento estiver arquivada. Nesses casos a sessão SHALL permanecer em "Não vinculadas", podendo ser vinculada manualmente.

#### Scenario: Lançamento vencido
- **WHEN** surge uma sessão que corresponderia a um lançamento registrado há 25 horas
- **THEN** a sessão não é registrada e aparece em "Não vinculadas"

#### Scenario: Demanda arquivada
- **WHEN** surge uma sessão que corresponderia a um lançamento cuja demanda foi arquivada
- **THEN** a sessão não é registrada e aparece em "Não vinculadas"

#### Scenario: Repositório removido
- **WHEN** o repositório do lançamento foi removido da demanda antes de a sessão surgir
- **THEN** o lançamento não existe mais, a sessão não é registrada e aparece em "Não vinculadas"

### Requirement: Escolha entre lançamentos pendentes
Quando mais de um lançamento pendente corresponder a uma sessão, o painel SHALL usar o mais recente deles. Quando houver mais de uma sessão a vincular na mesma leitura, o painel SHALL tratá-las da mais antiga para a mais nova, pela data de criação do arquivo de status.

#### Scenario: Lançamento abandonado em outra demanda
- **WHEN** há um lançamento pendente das 10:00 em `Faturamento` de `CHG-100`, sem sessão, e o usuário abre às 10:30 uma sessão do Codex em `Faturamento` de `CHG-142` e envia uma mensagem
- **THEN** a sessão é registrada em `CHG-142`, e o lançamento de `CHG-100` continua pendente

#### Scenario: Dois lançamentos no mesmo card
- **WHEN** o usuário abre duas sessões novas do Codex no mesmo card e envia uma mensagem em cada uma
- **THEN** as duas sessões ficam registradas nesse card, uma por lançamento

### Requirement: Registro removido não é refeito
Uma sessão excluída pelo usuário SHALL NOT ser vinculada automaticamente por nenhum lançamento nem voltar a aparecer no painel.

#### Scenario: Usuário remove uma sessão vinculada por lançamento
- **WHEN** o usuário exclui uma sessão que havia sido vinculada automaticamente e o arquivo de status dela continua existindo
- **THEN** a sessão não aparece em nenhum trabalho nem em "Sessões", e não é vinculada de novo

### Requirement: Cancelar lançamento pendente
O painel SHALL permitir cancelar um lançamento pendente pela linha dele no card, removendo-o do arquivo de dados sem pedir confirmação e sem fechar a aba aberta. Uma sessão que surja depois do cancelamento SHALL NOT ser vinculada por esse lançamento. Cancelar um lançamento que não existe mais SHALL NOT alterar o arquivo de dados.

#### Scenario: Aba fechada sem mensagem
- **WHEN** o usuário abre uma sessão nova do Codex, fecha a aba sem enviar mensagem e aciona "Cancelar" na linha do lançamento
- **THEN** a linha deixa de aparecer e o lançamento não consta mais no arquivo de dados

#### Scenario: Cancelar com a aba aberta
- **WHEN** o usuário cancela o lançamento e depois envia a primeira mensagem na aba que continuou aberta
- **THEN** a sessão aparece em "Não vinculadas" e pode ser vinculada manualmente

#### Scenario: Outro lançamento no mesmo card
- **WHEN** o card tem dois lançamentos pendentes e o usuário cancela um deles
- **THEN** o outro continua pendente e aparecendo no card

#### Scenario: Lançamento já usado por outra janela
- **WHEN** o usuário aciona "Cancelar" num lançamento que outra janela do painel acabou de usar para vincular uma sessão
- **THEN** o arquivo de dados não é alterado, a sessão vinculada permanece e a linha some na atualização seguinte

### Requirement: Retomar sessão não registrada
"Retomar" numa sessão de "Sessões" sem registro SHALL pedir o repositório entre as pastas diretas da raiz dos fontes, pré-selecionando a que tem o nome do último segmento da pasta de trabalho, e então SHALL registrar a sessão na demanda `sem-demanda`, nesse repositório, com `vinculadaEm`, e retomá-la. Só identificadores GUID canônicos SHALL ser aceitos.

#### Scenario: Retomar uma sessão do Codex aberta à mão
- **WHEN** o usuário aciona "Retomar" numa sessão do Codex sem registro, cuja pasta de trabalho termina em `Faturamento`, e confirma o repositório pré-selecionado
- **THEN** a sessão fica registrada em `Faturamento` em "Sessões" e abre uma aba com a conversa dela

#### Scenario: Repositório escolhido diferente
- **WHEN** o usuário escolhe `Estoque` para uma sessão cuja pasta de trabalho termina em `Faturamento`
- **THEN** o painel avisa que a retomada pode não encontrar a conversa e só registra e retoma se o usuário confirmar

#### Scenario: Escolha cancelada
- **WHEN** o usuário cancela a escolha do repositório
- **THEN** nada é registrado e nenhuma aba é aberta
