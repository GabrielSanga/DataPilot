# lista-de-sessoes Specification
## Purpose

Mostra numa única janela todas as sessões do Claude Code e do Codex e o estado de cada uma, para que o usuário veja de relance o que precisa dele sem percorrer as abas do terminal.

## Requirements
### Requirement: Lista de sessões
A janela SHALL mostrar cada sessão conhecida com o agente, o nome da pasta (último segmento da pasta de trabalho), o status exibido e o tempo decorrido desde a última atualização. Sessões registradas SHALL aparecer no card do seu repositório, dentro da sua demanda; as demais SHALL aparecer em "Não vinculadas". A pasta de trabalho completa e o identificador da sessão SHALL estar disponíveis para consulta na própria linha.

#### Scenario: Sessão do Claude em andamento
- **WHEN** existe uma sessão do Claude com pasta de trabalho `C:\Fontes\Faturamento` e status `executando` atualizado há 2 minutos
- **THEN** a linha dela mostra agente Claude, pasta `Faturamento`, status "executando" e a indicação de 2 minutos

#### Scenario: Sessões dos dois agentes
- **WHEN** uma demanda tem sessões do Claude e do Codex registradas
- **THEN** todas aparecem na mesma demanda, cada uma identificada pelo seu agente

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
As sessões de um card de repositório e as de "Não vinculadas" SHALL ser ordenadas pelo status exibido, nesta ordem: precisa de atenção, aguardando você, executando, ativa, inativa, encerrada, sem status. Dentro do mesmo status, a sessão atualizada mais recentemente SHALL vir primeiro.

#### Scenario: Sessão pede atenção
- **WHEN** uma sessão que estava "executando" passa a "precisa de atenção"
- **THEN** ela passa para o topo do seu card, acima das sessões nos demais status

#### Scenario: Duas sessões no mesmo status
- **WHEN** duas sessões do mesmo card estão "aguardando você"
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

### Requirement: Ação de retomar na linha
A linha de uma sessão SHALL oferecer a ação "Retomar" somente quando o identificador e o agente dessa sessão corresponderem a uma sessão registrada no arquivo de dados do painel. As demais linhas SHALL NOT oferecer a ação.

#### Scenario: Sessão aberta pelo painel
- **WHEN** a lista mostra uma sessão do Claude cujo identificador está registrado no arquivo de dados
- **THEN** a linha oferece "Retomar"

#### Scenario: Sessão do Codex
- **WHEN** a lista mostra uma sessão do Codex, que o painel não registra
- **THEN** a linha não oferece "Retomar"

#### Scenario: Sessão criada por /clear
- **WHEN** o usuário executa `/clear` numa sessão do Claude aberta pelo painel e surge uma sessão com outro identificador
- **THEN** a linha da sessão nova não oferece "Retomar", e a da sessão original continua oferecendo

#### Scenario: Arquivo de dados ilegível
- **WHEN** o arquivo de dados do painel não pode ser lido
- **THEN** nenhuma linha oferece "Retomar"

### Requirement: Repositório cadastrado na linha de sessão registrada
A linha de uma sessão registrada SHALL indicar o repositório em que ela foi registrada e o rótulo, quando houver, vindos do arquivo de dados do painel.

#### Scenario: Sessão registrada com rótulo
- **WHEN** a lista mostra uma sessão registrada em `Faturamento` com o rótulo "implementação"
- **THEN** a linha indica `Faturamento` e "implementação" como dados do painel

#### Scenario: Pasta de trabalho divergente
- **WHEN** uma sessão registrada em `Faturamento` tem, no arquivo de status, um `cwd` que aponta para outra pasta
- **THEN** a linha indica `Faturamento` como o repositório em que "Retomar" vai abrir

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

### Requirement: Seleção preservada nas atualizações
A demanda selecionada e a posição da lista SHALL ser mantidas quando a tela é atualizada por mudança de status, por releitura periódica ou por alteração de outra demanda.

#### Scenario: Status muda com uma demanda selecionada
- **WHEN** o status de uma sessão muda enquanto o usuário está com uma demanda selecionada
- **THEN** a mesma demanda continua selecionada e a tela mostra o novo status

#### Scenario: Demanda selecionada muda de posição
- **WHEN** a ordem das demandas muda porque outra demanda ficou mais urgente
- **THEN** a demanda que estava selecionada continua selecionada
