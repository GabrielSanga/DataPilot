# Spec Delta

## MODIFIED Requirements

### Requirement: Entrada "Não vinculadas"
A lista de demandas SHALL ter uma entrada "Não vinculadas" que mostra as sessões lidas da pasta de status que não correspondem a nenhuma sessão registrada, com a quantidade delas. Essas sessões SHALL oferecer a ação "Vincular" e nenhuma outra.

#### Scenario: Sessão do Codex
- **WHEN** existe uma sessão do Codex na pasta de status que o painel não registrou
- **THEN** ela aparece em "Não vinculadas", com a ação "Vincular" e sem "Retomar"

#### Scenario: Sessão criada por /clear
- **WHEN** o usuário executa `/clear` numa sessão registrada e surge uma sessão com outro identificador
- **THEN** a sessão nova aparece em "Não vinculadas", com a ação "Vincular", e a original continua na sua demanda

#### Scenario: Nenhuma sessão não vinculada
- **WHEN** todas as sessões da pasta de status estão registradas
- **THEN** a entrada "Não vinculadas" indica zero sessões

#### Scenario: Sessão vinculada sai da entrada
- **WHEN** o usuário vincula uma sessão de "Não vinculadas"
- **THEN** ela deixa de aparecer nessa entrada e a contagem diminui
