# Spec Delta

## ADDED Requirements

### Requirement: Lançamento pendente no card
O card de um repositório SHALL mostrar, depois das sessões registradas, uma linha para cada lançamento pendente válido desse repositório, do mais recente para o mais antigo, com o agente, o rótulo quando houver, o texto "aguardando a primeira mensagem" e o tempo decorrido desde o lançamento. A linha SHALL oferecer somente a ação "Cancelar". Lançamentos vencidos e lançamentos de demanda arquivada SHALL NOT ser mostrados.

#### Scenario: Codex recém-aberto
- **WHEN** o usuário abre uma sessão nova do Codex no card `Faturamento`, com o rótulo `ajuste`, e ainda não enviou mensagem
- **THEN** o card `Faturamento` mostra uma linha do Codex com o rótulo `ajuste` e "aguardando a primeira mensagem", sem "Retomar"

#### Scenario: Primeira mensagem enviada
- **WHEN** o usuário envia a primeira mensagem e a sessão é vinculada pelo lançamento
- **THEN** a linha do lançamento deixa de aparecer e a sessão aparece no mesmo card, na mesma atualização da tela

#### Scenario: Lançamento vencido
- **WHEN** um lançamento pendente foi registrado há mais de 24 horas
- **THEN** o card não mostra linha para ele

#### Scenario: Demanda arquivada
- **WHEN** o usuário mostra as demandas arquivadas e seleciona uma que tem um lançamento pendente
- **THEN** o card não mostra linha para esse lançamento

#### Scenario: Dois lançamentos no mesmo card
- **WHEN** o usuário abre duas sessões novas do Codex no mesmo card e não envia mensagem em nenhuma
- **THEN** o card mostra duas linhas de lançamento, a mais recente primeiro

#### Scenario: Seleção mantida
- **WHEN** uma linha de lançamento surge, some ou é substituída pela sessão enquanto o usuário está com uma demanda selecionada
- **THEN** a mesma demanda continua selecionada

### Requirement: Lançamento pendente fora das contagens
Os lançamentos pendentes SHALL NOT entrar na quantidade de sessões de uma demanda, no status mais urgente dela, na ordem da lista de demandas nem no resumo do cabeçalho.

#### Scenario: Demanda só com lançamento pendente
- **WHEN** uma demanda tem um repositório, nenhuma sessão registrada e um lançamento pendente
- **THEN** o resumo dela indica 1 repositório e 0 sessões, sem status

#### Scenario: Ordem das demandas depois de um clique
- **WHEN** o usuário abre uma sessão nova do Codex numa demanda e ainda não enviou mensagem
- **THEN** a posição dessa demanda na lista não muda
