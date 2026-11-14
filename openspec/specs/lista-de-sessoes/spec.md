# lista-de-sessoes Specification
## Purpose

Mostra numa única janela todas as sessões do Claude Code e do Codex e o estado de cada uma, para que o usuário veja de relance o que precisa dele sem percorrer as abas do terminal.

## Requirements
### Requirement: Lista de sessões
A janela SHALL listar cada sessão conhecida com o agente, o nome da pasta (último segmento da pasta de trabalho), o status exibido e o tempo decorrido desde a última atualização. A pasta de trabalho completa e o identificador da sessão SHALL estar disponíveis para consulta na própria lista.

#### Scenario: Sessão do Claude em andamento
- **WHEN** existe uma sessão do Claude com pasta de trabalho `C:\Fontes\Faturamento` e status `executando` atualizado há 2 minutos
- **THEN** a lista mostra uma linha com agente Claude, pasta `Faturamento`, status "executando" e a indicação de 2 minutos

#### Scenario: Sessões dos dois agentes
- **WHEN** existem sessões do Claude e do Codex
- **THEN** todas aparecem na mesma lista, cada uma identificada pelo seu agente

### Requirement: Status exibido com regra de inatividade
O status exibido SHALL ser o status gravado, exceto: `ativa` ou `executando` sem atualização há mais de 15 minutos SHALL ser exibido como "inativa"; `aguardando` ou `atencao` sem atualização há mais de 8 horas SHALL ser exibido como "inativa". `encerrada` SHALL ser exibido sempre como "encerrada". A regra vale igualmente para os dois agentes.

#### Scenario: Sessão executando sem eventos há 20 minutos
- **WHEN** uma sessão tem status `executando` e a última atualização foi há 20 minutos
- **THEN** ela é exibida como "inativa"

#### Scenario: Sessão executando com evento recente
- **WHEN** uma sessão tem status `executando` e a última atualização foi há 10 minutos
- **THEN** ela é exibida como "executando"

#### Scenario: Sessão aguardando há 2 horas
- **WHEN** uma sessão tem status `aguardando` e a última atualização foi há 2 horas
- **THEN** ela é exibida como "aguardando você"

#### Scenario: Sessão do Codex cuja aba foi fechada
- **WHEN** uma sessão do Codex ficou com status `aguardando` e a última atualização foi há 9 horas
- **THEN** ela é exibida como "inativa"

#### Scenario: Pedido de atenção antigo
- **WHEN** uma sessão tem status `atencao` e a última atualização foi há 9 horas
- **THEN** ela é exibida como "inativa"

#### Scenario: Sessão passa a inativa com o painel aberto
- **WHEN** uma sessão `executando` completa 15 minutos sem atualização enquanto o painel está aberto
- **THEN** o status exibido muda para "inativa" sem ação do usuário

#### Scenario: Atualização com data no futuro
- **WHEN** o instante de atualização de uma sessão é posterior à hora atual da máquina
- **THEN** a sessão é tratada como atualizada agora

### Requirement: Cores por status
Cada status exibido SHALL ter uma cor: vermelho para "precisa de atenção", azul para "executando", amarelo para "aguardando você", verde para "ativa" e cinza para "encerrada" e "inativa". O nome do status SHALL aparecer em texto, de modo que a cor não seja a única indicação.

#### Scenario: Sessão pedindo permissão
- **WHEN** uma sessão é exibida como "precisa de atenção"
- **THEN** o indicador de status dela é vermelho e traz o texto do status

#### Scenario: Sessão inativa
- **WHEN** uma sessão é exibida como "inativa"
- **THEN** o indicador de status dela é cinza e traz o texto do status

### Requirement: Ordem por urgência
A lista SHALL ser ordenada pelo status exibido, nesta ordem: precisa de atenção, aguardando você, executando, ativa, inativa, encerrada. Dentro do mesmo status, a sessão atualizada mais recentemente SHALL vir primeiro.

#### Scenario: Sessão pede atenção
- **WHEN** uma sessão que estava "executando" passa a "precisa de atenção"
- **THEN** ela passa para o topo da lista, acima das sessões nos demais status

#### Scenario: Duas sessões no mesmo status
- **WHEN** duas sessões estão "aguardando você"
- **THEN** a atualizada mais recentemente aparece acima da outra

### Requirement: Resumo no cabeçalho
O cabeçalho SHALL mostrar quantas sessões estão em "precisa de atenção", "aguardando você" e "executando", omitindo os status com contagem zero.

#### Scenario: Sessões em vários status
- **WHEN** há uma sessão em "precisa de atenção" e duas em "executando"
- **THEN** o cabeçalho mostra "1 precisa de atenção" e "2 executando"

#### Scenario: Nenhuma sessão pede o usuário
- **WHEN** não há sessões em "precisa de atenção", "aguardando você" nem "executando"
- **THEN** o cabeçalho indica que nada precisa do usuário

### Requirement: Sessões antigas ocultas por padrão
Sessões exibidas como "encerrada" ou "inativa" cuja última atualização tenha mais de 3 dias SHALL ficar ocultas, a menos que o usuário ative a opção de mostrar sessões antigas. Sessões ocultas SHALL NOT entrar no resumo do cabeçalho.

#### Scenario: Sessão encerrada há uma semana
- **WHEN** uma sessão foi encerrada há 7 dias e a opção de mostrar antigas está desativada
- **THEN** ela não aparece na lista

#### Scenario: Usuário pede para ver as antigas
- **WHEN** o usuário ativa a opção de mostrar sessões antigas
- **THEN** as sessões encerradas ou inativas há mais de 3 dias passam a aparecer

#### Scenario: Sessão encerrada ontem
- **WHEN** uma sessão foi encerrada há 1 dia
- **THEN** ela aparece na lista, mesmo com a opção de mostrar antigas desativada

### Requirement: Texto dos arquivos exibido como texto simples
Os valores vindos dos arquivos de status SHALL ser exibidos como texto simples, sem caracteres de controle e limitados a 260 caracteres, com indicação de corte quando excederem.

#### Scenario: Pasta de trabalho muito longa
- **WHEN** o `cwd` de uma sessão tem mais de 260 caracteres
- **THEN** o texto exibido é cortado em 260 caracteres com indicação de corte, e a janela mantém o leiaute

#### Scenario: Pasta de trabalho com quebras de linha
- **WHEN** o `cwd` de uma sessão contém quebras de linha ou outros caracteres de controle
- **THEN** esses caracteres não aparecem no texto exibido

### Requirement: Aviso de pasta de status indisponível
Quando a pasta de status não puder ser lida, a janela SHALL mostrar um aviso que diga qual pasta está indisponível, em vez de uma lista vazia sem explicação.

#### Scenario: Pasta inexistente
- **WHEN** o painel é aberto numa máquina em que a pasta de status não existe
- **THEN** a janela mostra o aviso com o caminho da pasta

#### Scenario: Pasta existente e vazia
- **WHEN** a pasta de status existe e não há sessões
- **THEN** a janela indica que não há sessões, sem o aviso de pasta indisponível

### Requirement: Pasta de status definida na inicialização
O painel SHALL ler `C:\ProgramData\Painel\sessoes` por padrão e SHALL aceitar outra pasta informada pelo usuário na linha de comando ao iniciar.

#### Scenario: Início sem parâmetros
- **WHEN** o painel é iniciado sem parâmetros
- **THEN** ele lê `C:\ProgramData\Painel\sessoes`

#### Scenario: Pasta informada na linha de comando
- **WHEN** o painel é iniciado com `--pasta-status` seguido de um caminho
- **THEN** ele lê as sessões dessa pasta
