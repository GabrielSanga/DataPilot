# Spec Delta

## ADDED Requirements

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

## MODIFIED Requirements

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

### Requirement: Registro removido não é refeito
Uma sessão excluída pelo usuário SHALL NOT ser vinculada automaticamente por nenhum lançamento nem voltar a aparecer no painel.

#### Scenario: Usuário remove uma sessão vinculada por lançamento
- **WHEN** o usuário exclui uma sessão que havia sido vinculada automaticamente e o arquivo de status dela continua existindo
- **THEN** a sessão não aparece em nenhum trabalho nem em "Sessões", e não é vinculada de novo

## REMOVED Requirements

### Requirement: Desfazer o vínculo
**Reason**: o registro de uma sessão não é mais removido para devolvê-la à lista; a sessão é excluída do painel.
**Migration**: usar "Excluir sessão"; para tirar uma sessão de "Sessões" e pô-la num trabalho, usar "Vincular a um trabalho".
