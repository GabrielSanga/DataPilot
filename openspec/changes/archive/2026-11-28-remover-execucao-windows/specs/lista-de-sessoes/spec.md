# Spec Delta

## MODIFIED Requirements

### Requirement: Lista de sessões
A janela SHALL mostrar cada sessão conhecida com o agente, o nome da pasta (último segmento da pasta de trabalho), o status exibido e o tempo decorrido desde a última atualização. Sessões registradas SHALL aparecer no card do seu repositório, dentro da sua demanda; as demais SHALL aparecer em "Não vinculadas". A pasta de trabalho completa e o identificador da sessão SHALL estar disponíveis para consulta na própria linha.

#### Scenario: Sessão do Claude em andamento
- **WHEN** existe uma sessão do Claude com pasta de trabalho `/workspace/Faturamento` e status `executando` atualizado há 2 minutos
- **THEN** a linha dela mostra agente Claude, pasta `Faturamento`, status "executando" e a indicação de 2 minutos

#### Scenario: Sessões dos dois agentes
- **WHEN** uma demanda tem sessões do Claude e do Codex registradas
- **THEN** todas aparecem na mesma demanda, cada uma identificada pelo seu agente
