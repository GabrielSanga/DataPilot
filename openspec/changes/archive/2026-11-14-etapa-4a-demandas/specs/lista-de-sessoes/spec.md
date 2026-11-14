# Spec Delta

## ADDED Requirements

### Requirement: Tela agrupada por demanda
A janela SHALL ter duas áreas: uma lista de demandas e a demanda selecionada. A demanda selecionada SHALL mostrar o `id`, o título, o `openspecChange` quando houver, e um card por repositório com as sessões registradas nele.

#### Scenario: Demanda com dois repositórios e os dois agentes
- **WHEN** a demanda selecionada tem `Faturamento` com uma sessão do Claude e `Estoque` com uma sessão do Codex
- **THEN** a área da demanda mostra dois cards, cada um com a sessão do seu repositório

#### Scenario: Demanda sem repositórios
- **WHEN** a demanda selecionada não tem repositórios
- **THEN** a área da demanda orienta o usuário a vincular um repositório

#### Scenario: Nenhuma demanda
- **WHEN** o arquivo de dados não tem demandas
- **THEN** a janela orienta o usuário a criar uma demanda

### Requirement: Resumo e ordem das demandas
Cada demanda da lista SHALL mostrar a quantidade de repositórios, a quantidade de sessões e o status exibido mais urgente entre suas sessões. A lista SHALL ser ordenada pelo status mais urgente de cada demanda e, em caso de empate, pela atualização mais recente; demandas sem sessões com status SHALL vir depois, e as arquivadas por último.

#### Scenario: Demanda com sessão pedindo atenção
- **WHEN** uma demanda tem duas sessões, uma "executando" e outra "precisa de atenção"
- **THEN** o resumo dela indica "precisa de atenção" e ela aparece acima das demandas cujo status mais urgente é menos urgente

#### Scenario: Demanda sem sessões
- **WHEN** uma demanda tem um repositório e nenhuma sessão
- **THEN** o resumo indica 1 repositório e 0 sessões, sem status

### Requirement: Sessões registradas sempre visíveis na demanda
Toda sessão registrada SHALL aparecer no card do seu repositório, independentemente da idade. Uma sessão registrada que não tenha arquivo de status SHALL ser exibida como "sem status", em cinza, depois das demais, e SHALL oferecer a ação de retomar.

#### Scenario: Sessão encerrada há uma semana
- **WHEN** uma sessão registrada foi encerrada há 7 dias
- **THEN** ela continua aparecendo no card do repositório, com a ação de retomar

#### Scenario: Sessão sem arquivo de status
- **WHEN** uma sessão está registrada no arquivo de dados e não existe arquivo de status para ela
- **THEN** ela aparece no card do repositório como "sem status" e pode ser retomada

### Requirement: Entrada "Não vinculadas"
A lista de demandas SHALL ter uma entrada "Não vinculadas" que mostra as sessões lidas da pasta de status que não correspondem a nenhuma sessão registrada, com a quantidade delas. Essas sessões SHALL NOT oferecer ações.

#### Scenario: Sessão do Codex
- **WHEN** existe uma sessão do Codex na pasta de status que o painel não registrou
- **THEN** ela aparece em "Não vinculadas", sem ações

#### Scenario: Sessão criada por /clear
- **WHEN** o usuário executa `/clear` numa sessão registrada e surge uma sessão com outro identificador
- **THEN** a sessão nova aparece em "Não vinculadas" e a original continua na sua demanda

#### Scenario: Nenhuma sessão não vinculada
- **WHEN** todas as sessões da pasta de status estão registradas
- **THEN** a entrada "Não vinculadas" indica zero sessões

### Requirement: Seleção preservada nas atualizações
A demanda selecionada e a posição da lista SHALL ser mantidas quando a tela é atualizada por mudança de status, por releitura periódica ou por alteração de outra demanda.

#### Scenario: Status muda com uma demanda selecionada
- **WHEN** o status de uma sessão muda enquanto o usuário está com uma demanda selecionada
- **THEN** a mesma demanda continua selecionada e a tela mostra o novo status

#### Scenario: Demanda selecionada muda de posição
- **WHEN** a ordem das demandas muda porque outra demanda ficou mais urgente
- **THEN** a demanda que estava selecionada continua selecionada

## MODIFIED Requirements

### Requirement: Lista de sessões
A janela SHALL mostrar cada sessão conhecida com o agente, o nome da pasta (último segmento da pasta de trabalho), o status exibido e o tempo decorrido desde a última atualização. Sessões registradas SHALL aparecer no card do seu repositório, dentro da sua demanda; as demais SHALL aparecer em "Não vinculadas". A pasta de trabalho completa e o identificador da sessão SHALL estar disponíveis para consulta na própria linha.

#### Scenario: Sessão do Claude em andamento
- **WHEN** existe uma sessão do Claude com pasta de trabalho `C:\Fontes\Faturamento` e status `executando` atualizado há 2 minutos
- **THEN** a linha dela mostra agente Claude, pasta `Faturamento`, status "executando" e a indicação de 2 minutos

#### Scenario: Sessões dos dois agentes
- **WHEN** uma demanda tem sessões do Claude e do Codex registradas
- **THEN** todas aparecem na mesma demanda, cada uma identificada pelo seu agente

### Requirement: Ordem por urgência
As sessões de um card de repositório e as de "Não vinculadas" SHALL ser ordenadas pelo status exibido, nesta ordem: precisa de atenção, aguardando você, executando, ativa, inativa, encerrada, sem status. Dentro do mesmo status, a sessão atualizada mais recentemente SHALL vir primeiro.

#### Scenario: Sessão pede atenção
- **WHEN** uma sessão que estava "executando" passa a "precisa de atenção"
- **THEN** ela passa para o topo do seu card, acima das sessões nos demais status

#### Scenario: Duas sessões no mesmo status
- **WHEN** duas sessões do mesmo card estão "aguardando você"
- **THEN** a atualizada mais recentemente aparece acima da outra

### Requirement: Sessões antigas ocultas por padrão
Em "Não vinculadas", sessões exibidas como "encerrada" ou "inativa" cuja última atualização tenha mais de 3 dias SHALL ficar ocultas, a menos que o usuário ative a opção de mostrar sessões antigas. Sessões ocultas SHALL NOT entrar no resumo do cabeçalho nem na contagem de "Não vinculadas".

#### Scenario: Sessão encerrada há uma semana
- **WHEN** uma sessão não vinculada foi encerrada há 7 dias e a opção de mostrar antigas está desativada
- **THEN** ela não aparece em "Não vinculadas"

#### Scenario: Usuário pede para ver as antigas
- **WHEN** o usuário ativa a opção de mostrar sessões antigas
- **THEN** as sessões não vinculadas encerradas ou inativas há mais de 3 dias passam a aparecer

#### Scenario: Sessão encerrada ontem
- **WHEN** uma sessão não vinculada foi encerrada há 1 dia
- **THEN** ela aparece em "Não vinculadas", mesmo com a opção de mostrar antigas desativada
