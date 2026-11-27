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
Cada status exibido SHALL ter uma cor no indicador da linha: vermelho para "precisa de atenção", azul para "executando", amarelo para "aguardando você", verde para "ativa" e cinza para "encerrada" e "inativa". O nome do status SHALL estar na dica da linha e no nome acessível do indicador, e SHALL aparecer em texto quando o alto contraste do Windows estiver ligado.

#### Scenario: Sessão pedindo permissão
- **WHEN** uma sessão é exibida como "precisa de atenção"
- **THEN** o indicador dela é vermelho e a dica e o nome acessível trazem "precisa de atenção"

#### Scenario: Sessão inativa
- **WHEN** uma sessão é exibida como "inativa"
- **THEN** o indicador dela é cinza e a dica e o nome acessível trazem "inativa"

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
O painel SHALL ler `C:\ProgramData\Painel\sessoes` por padrão e SHALL aceitar outra pasta informada pelo usuário na linha de comando ao iniciar. Se a pasta não existir, o painel SHALL criá-la antes de começar a ler; se não conseguir, SHALL mostrar o aviso de pasta indisponível. O painel SHALL NOT criar arquivos dentro dela.

#### Scenario: Início sem parâmetros
- **WHEN** o painel é iniciado sem parâmetros
- **THEN** ele lê `C:\ProgramData\Painel\sessoes`

#### Scenario: Pasta informada na linha de comando
- **WHEN** o painel é iniciado com `--pasta-status` seguido de um caminho
- **THEN** ele lê as sessões dessa pasta

#### Scenario: Pasta que não existe
- **WHEN** o painel é iniciado e a pasta de status não existe
- **THEN** a pasta é criada, vazia, e a janela indica que não há sessões

#### Scenario: Pasta que não pode ser criada
- **WHEN** a pasta de status não existe e o usuário não tem permissão para criá-la
- **THEN** a janela mostra o aviso de pasta indisponível

### Requirement: Ação de retomar na linha
A linha de uma sessão registrada SHALL oferecer "Retomar" com o repositório do registro. A linha de uma sessão de "Sessões" sem registro SHALL oferecer "Retomar" quando o identificador for um GUID canônico, pedindo o repositório antes. Com o arquivo de dados ilegível, nenhuma linha SHALL oferecer "Retomar".

#### Scenario: Sessão aberta pelo painel
- **WHEN** a lista mostra uma sessão do Claude cujo identificador está registrado no arquivo de dados
- **THEN** a linha oferece "Retomar", que abre no repositório do registro

#### Scenario: Sessão do Codex
- **WHEN** a lista mostra em "Sessões" uma sessão do Codex que o painel não registrou
- **THEN** a linha oferece "Retomar", que pede o repositório antes de abrir

#### Scenario: Sessão criada por /clear
- **WHEN** o usuário executa `/clear` numa sessão do Claude aberta pelo painel e surge uma sessão com outro identificador
- **THEN** a sessão nova aparece em "Sessões" com "Retomar", e a original continua com "Retomar" no seu lugar

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
A janela SHALL ter duas áreas: a lista, com os trabalhos e a entrada "Sessões", e o item selecionado. O trabalho selecionado SHALL mostrar o título e um card por repositório com a change e o progresso dele e as sessões registradas nele.

#### Scenario: Demanda com dois repositórios e os dois agentes
- **WHEN** o trabalho selecionado tem `Faturamento` com uma sessão do Claude e `Estoque` com uma sessão do Codex
- **THEN** a área do trabalho mostra dois cards, cada um com a sessão do seu repositório

#### Scenario: Demanda sem repositórios
- **WHEN** o trabalho selecionado não tem repositórios
- **THEN** a área do trabalho orienta o usuário a vincular um repositório

#### Scenario: Nenhuma demanda
- **WHEN** o arquivo de dados não tem trabalhos
- **THEN** a janela orienta o usuário a criar um trabalho ou a usar "Sessões"

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

### Requirement: Seleção preservada nas atualizações
A demanda selecionada e a posição da lista SHALL ser mantidas quando a tela é atualizada por mudança de status, por releitura periódica ou por alteração de outra demanda.

#### Scenario: Status muda com uma demanda selecionada
- **WHEN** o status de uma sessão muda enquanto o usuário está com uma demanda selecionada
- **THEN** a mesma demanda continua selecionada e a tela mostra o novo status

#### Scenario: Demanda selecionada muda de posição
- **WHEN** a ordem das demandas muda porque outra demanda ficou mais urgente
- **THEN** a demanda que estava selecionada continua selecionada

### Requirement: Lançamento pendente no card
O card de um repositório SHALL mostrar, depois das sessões registradas, uma linha para cada lançamento pendente válido desse repositório, do mais recente para o mais antigo, com o agente, o rótulo quando houver, o texto "aguardando a primeira mensagem" e o tempo decorrido desde o lançamento. A linha SHALL oferecer as ações "Abrir" e "Cancelar", e nenhuma outra. Lançamentos vencidos e lançamentos de demanda arquivada SHALL NOT ser mostrados.

#### Scenario: Codex recém-aberto
- **WHEN** o usuário abre uma sessão nova do Codex no card `Faturamento`, com o rótulo `ajuste`, e ainda não enviou mensagem
- **THEN** o card `Faturamento` mostra uma linha do Codex com o rótulo `ajuste` e "aguardando a primeira mensagem", com "Abrir" e "Cancelar" e sem "Retomar"

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

### Requirement: Alerta do que precisa do usuário
O topo da janela SHALL destacar em vermelho a quantidade de sessões em "precisa de atenção". Sem nenhuma nesse status e com sessões em "aguardando você", SHALL destacar em amarelo a quantidade delas. Sem nenhuma das duas, SHALL indicar sem destaque que nada precisa do usuário. O destaque SHALL usar o mesmo conjunto de sessões do resumo do cabeçalho.

#### Scenario: Sessões pedindo atenção
- **WHEN** duas sessões estão em "precisa de atenção" e quatro em "aguardando você"
- **THEN** o topo mostra em vermelho "2 precisam de atenção", com "Ir para a próxima"

#### Scenario: Só sessões aguardando
- **WHEN** nenhuma sessão está em "precisa de atenção" e uma está em "aguardando você"
- **THEN** o topo mostra em amarelo "1 aguardando você", com "Ir para a próxima"

#### Scenario: Nada precisa do usuário
- **WHEN** nenhuma sessão está em "precisa de atenção" nem em "aguardando você"
- **THEN** o topo mostra "Nada precisa de você agora", sem destaque e sem "Ir para a próxima"

### Requirement: Ir para a próxima sessão que precisa do usuário
"Ir para a próxima", e a tecla F8, SHALL selecionar o próximo item da lista, depois do selecionado e voltando ao início, que tenha sessão no status destacado no topo. Uma sessão de "Sessões" nesse status SHALL levar a "Sessões". Sessões excluídas SHALL NOT contar. Sem sessão no status destacado, a ação SHALL NOT estar disponível.

#### Scenario: Duas demandas pedindo atenção
- **WHEN** dois trabalhos têm sessões em "precisa de atenção", o primeiro está selecionado e o usuário aciona "Ir para a próxima"
- **THEN** o segundo trabalho passa a ser o selecionado

#### Scenario: Fim da lista
- **WHEN** o último item com sessão em "precisa de atenção" está selecionado e o usuário pressiona F8
- **THEN** o primeiro item da lista com sessão nesse status passa a ser o selecionado

#### Scenario: Sessão não vinculada pedindo atenção
- **WHEN** a única sessão em "precisa de atenção" está em "Sessões"
- **THEN** "Ir para a próxima" seleciona "Sessões"

### Requirement: Filtro de demandas
Um campo acima da lista SHALL filtrar os trabalhos exibidos pelo texto digitado, comparando com o título, os nomes dos repositórios e as changes deles, sem diferenciar maiúsculas nem acentos. "Sessões" SHALL continuar visível. Ctrl+F SHALL levar o foco ao campo e Esc, no campo, SHALL limpar o filtro. Filtrar SHALL NOT mudar o item selecionado.

#### Scenario: Filtro pelo repositório
- **WHEN** o usuário digita "estoque" no filtro
- **THEN** a lista mostra só os trabalhos que têm o repositório `Estoque` ou esse texto no título ou numa change, e "Sessões"

#### Scenario: Filtro sem acento
- **WHEN** o usuário digita "relatorio" e existe o trabalho "Relatório mensal"
- **THEN** esse trabalho aparece na lista

#### Scenario: Limpar o filtro
- **WHEN** o foco está no filtro e o usuário pressiona Esc
- **THEN** o filtro fica vazio e todos os trabalhos voltam a aparecer

#### Scenario: Demanda selecionada fora do filtro
- **WHEN** o usuário digita um texto que não casa com o trabalho selecionado
- **THEN** a área continua mostrando o trabalho selecionado

### Requirement: Nenhuma demanda selecionada
Com nenhuma demanda selecionada, a área da demanda SHALL mostrar só a orientação para selecionar ou criar uma demanda, sem ações, sem progresso e sem textos de uma demanda.

#### Scenario: Painel recém-aberto
- **WHEN** o painel abre e nenhuma demanda está selecionada
- **THEN** a área da demanda mostra só a orientação, sem botões, barra de progresso ou o texto de demanda sem repositórios

### Requirement: Destaque das sessões que precisam do usuário
As linhas de sessões em "precisa de atenção" e "aguardando você" SHALL ter uma faixa na cor do status junto à borda esquerda. As linhas nos demais status SHALL ter só o indicador, sem faixa. Um item da lista com sessão em um desses dois status SHALL ter a mesma faixa, na cor do status mais urgente.

#### Scenario: Card com sessões em vários status
- **WHEN** um card tem uma sessão "precisa de atenção", uma "aguardando você" e uma "executando"
- **THEN** as duas primeiras têm faixa vermelha e amarela, e a terceira tem só o indicador azul, sem faixa

#### Scenario: Demanda pedindo atenção na lista
- **WHEN** um trabalho tem uma sessão em "precisa de atenção"
- **THEN** o item dele na lista tem a faixa vermelha

### Requirement: Ação principal e menu de ações
Sessões SHALL mostrar "Retomar" e "Excluir"; as de "Sessões" SHALL ter, no menu "Mais ações", "Vincular a um trabalho", e as de um trabalho SHALL NOT ter menu. Lançamento pendente SHALL mostrar "Abrir" e, no menu, "Cancelar"; repositório, "Nova sessão" e, no menu, "Trocar change" e "Remover repositório"; trabalho, "Vincular repositório" e, no menu, "Editar" e "Arquivar" ou "Desarquivar".

#### Scenario: Linha de sessão registrada
- **WHEN** a lista mostra uma sessão registrada num trabalho
- **THEN** a linha mostra "Retomar" e o botão "Excluir", sem menu de mais ações

#### Scenario: Menu pelo teclado
- **WHEN** o foco está no cabeçalho de um repositório e o usuário pressiona Shift+F10
- **THEN** o menu de mais ações do repositório abre e pode ser percorrido pelas setas

#### Scenario: Sessão não vinculada
- **WHEN** a lista mostra uma sessão em "Sessões"
- **THEN** a linha mostra "Retomar", "Excluir" e o menu com "Vincular a um trabalho"

#### Scenario: Vincular indisponível
- **WHEN** uma sessão de "Sessões" tem identificador fora do formato GUID
- **THEN** "Retomar" e "Vincular a um trabalho" ficam indisponíveis, o motivo aparece em texto na linha e "Excluir" continua disponível

### Requirement: Entrada "Sessões"
A lista SHALL ter, no fim, a entrada "Sessões", com a quantidade e as linhas das sessões registradas na demanda `sem-demanda`, com o repositório de cada uma, dos lançamentos pendentes dela e das sessões da pasta de status não registradas em nenhuma demanda nem excluídas. A área SHALL oferecer "Nova sessão", que pede a pasta do repositório antes do agente.

#### Scenario: Sessão do Codex aberta fora do painel
- **WHEN** existe uma sessão do Codex na pasta de status que o painel não registrou
- **THEN** ela aparece em "Sessões", com "Retomar" e "Excluir"

#### Scenario: Sessão aberta sem trabalho
- **WHEN** o usuário aciona "Nova sessão" em "Sessões", escolhe a pasta `Faturamento` e o Claude
- **THEN** a sessão abre no terminal e aparece em "Sessões", indicando `Faturamento`

#### Scenario: Sessão vinculada a um trabalho sai da entrada
- **WHEN** o usuário vincula uma sessão de "Sessões" a um trabalho
- **THEN** ela deixa de aparecer em "Sessões" e a contagem diminui

#### Scenario: Nenhuma sessão fora de trabalhos
- **WHEN** todas as sessões estão em trabalhos ou foram excluídas
- **THEN** "Sessões" indica zero sessões e orienta a usar "Nova sessão"

### Requirement: Linha da sessão
A linha de uma sessão SHALL mostrar, nesta ordem, um indicador redondo na cor do status, o agente, o rótulo, o tempo decorrido e as ações. Sem rótulo, o lugar dele SHALL ficar em branco.

#### Scenario: Sessão sem rótulo
- **WHEN** a lista mostra uma sessão sem rótulo
- **THEN** o lugar do rótulo fica vazio, sem texto como "sem rótulo"

#### Scenario: Sessão com rótulo
- **WHEN** a lista mostra uma sessão com o rótulo "implementação"
- **THEN** a linha mostra o indicador, o agente, "implementação", o tempo e as ações
