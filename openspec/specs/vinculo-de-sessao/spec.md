# vinculo-de-sessao Specification
## Purpose

Permite associar a uma demanda uma sessão que o painel não abriu, como as do Codex e as criadas por `/clear`, para que ela apareça na demanda e possa ser retomada, deixando passar do arquivo de status para os dados do painel apenas o mínimo necessário e sempre por decisão do usuário.

## Requirements
### Requirement: Vincular sessão não vinculada
O painel SHALL permitir vincular uma sessão de "Não vinculadas" a uma demanda não arquivada, em um repositório dessa demanda ou em um repositório novo informado pelo usuário, com um rótulo opcional. Depois de vinculada, a sessão SHALL constar no arquivo de dados, aparecer no card do repositório com seu status e poder ser retomada.

#### Scenario: Sessão do Codex
- **WHEN** o usuário vincula uma sessão do Codex ao repositório `Faturamento` da demanda `CHG-142`
- **THEN** a sessão passa a aparecer no card `Faturamento` de `CHG-142`, com o status atual, e oferece "Retomar"

#### Scenario: Sessão criada por /clear
- **WHEN** o usuário vincula a sessão que surgiu depois de um `/clear` numa sessão do Claude
- **THEN** a sessão nova passa a aparecer na demanda escolhida e pode ser retomada

#### Scenario: Retomar sessão vinculada
- **WHEN** o usuário aciona "Retomar" numa sessão do Codex vinculada
- **THEN** abre uma aba com a conversa anterior do Codex, no repositório escolhido no vínculo

#### Scenario: Nenhuma demanda disponível
- **WHEN** o usuário aciona "Vincular" e não existe demanda não arquivada
- **THEN** o painel orienta a criar uma demanda e não grava nada

### Requirement: Somente identificador e agente cruzam
No vínculo, os únicos valores do arquivo de status gravados no arquivo de dados SHALL ser o identificador da sessão e o agente. A demanda, o repositório e o rótulo SHALL vir da escolha do usuário. A pasta de trabalho, o status, o evento e a data do arquivo de status SHALL NOT ser gravados.

#### Scenario: Pasta de trabalho diferente do repositório escolhido
- **WHEN** o usuário vincula ao repositório `Faturamento` uma sessão cujo arquivo de status traz outra pasta de trabalho
- **THEN** a sessão é gravada em `Faturamento`, e a pasta de trabalho do arquivo de status não consta no arquivo de dados

#### Scenario: Conteúdo gravado
- **WHEN** uma sessão é vinculada
- **THEN** o registro dela no arquivo de dados tem o agente, o identificador, o rótulo quando informado e as datas, e nenhum outro valor vindo do arquivo de status

### Requirement: Somente GUID canônico pode ser vinculado
O painel SHALL permitir vincular apenas sessões cujo identificador seja um GUID canônico (8-4-4-4-12 dígitos hexadecimais). Para as demais, a ação SHALL ficar indisponível, com a indicação do motivo.

#### Scenario: Identificador fora do formato
- **WHEN** uma sessão de "Não vinculadas" tem um identificador que não é um GUID canônico
- **THEN** a ação de vincular não está disponível para ela e o painel indica que o identificador não pode ser usado para retomar

### Requirement: Vínculo apenas por ação explícita
O painel SHALL NOT vincular nenhuma sessão sem uma ação do usuário sobre essa sessão, inclusive sessões do Codex abertas pelo próprio painel.

#### Scenario: Codex aberto pelo painel
- **WHEN** o usuário abre uma sessão nova do Codex pelo card de um repositório e o arquivo de status dela surge
- **THEN** o arquivo de dados não é alterado até que o usuário acione o vínculo

#### Scenario: Sessão não vinculada na pasta de um repositório da demanda
- **WHEN** existe uma sessão não vinculada cuja pasta de trabalho tem o mesmo nome de um repositório de uma demanda
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
Uma sessão vinculada SHALL ser gravada com o campo `vinculadaEm`, com a data e hora do vínculo. Sessões abertas pelo painel SHALL NOT ter esse campo.

#### Scenario: Sessão vinculada
- **WHEN** o usuário vincula uma sessão
- **THEN** o registro dela no arquivo de dados tem `vinculadaEm`

#### Scenario: Sessão aberta pelo painel
- **WHEN** o usuário abre uma sessão nova do Claude pelo painel
- **THEN** o registro dela no arquivo de dados não tem `vinculadaEm`

### Requirement: Sessão já registrada não é vinculada de novo
O painel SHALL recusar o vínculo de uma sessão cujo identificador já conste no arquivo de dados.

#### Scenario: Vínculo repetido
- **WHEN** o usuário tenta vincular uma sessão que já foi registrada por outra janela do painel
- **THEN** o vínculo é recusado com uma mensagem e o arquivo de dados não é alterado

### Requirement: Desfazer o vínculo
Remover o registro de uma sessão vinculada SHALL fazê-la voltar a "Não vinculadas", enquanto existir arquivo de status para ela.

#### Scenario: Registro removido
- **WHEN** o usuário remove o registro de uma sessão vinculada
- **THEN** a sessão deixa de aparecer na demanda e volta a aparecer em "Não vinculadas", podendo ser vinculada de novo
