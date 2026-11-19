# Spec Delta

## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Cores por status
Cada status exibido SHALL ter uma cor no indicador da linha: vermelho para "precisa de atenção", azul para "executando", amarelo para "aguardando você", verde para "ativa" e cinza para "encerrada" e "inativa". O nome do status SHALL estar na dica da linha e no nome acessível do indicador, e SHALL aparecer em texto quando o alto contraste do Windows estiver ligado.

#### Scenario: Sessão pedindo permissão
- **WHEN** uma sessão é exibida como "precisa de atenção"
- **THEN** o indicador dela é vermelho e a dica e o nome acessível trazem "precisa de atenção"

#### Scenario: Sessão inativa
- **WHEN** uma sessão é exibida como "inativa"
- **THEN** o indicador dela é cinza e a dica e o nome acessível trazem "inativa"

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

## REMOVED Requirements

### Requirement: Entrada "Não vinculadas"
**Reason**: a entrada passa a reunir também as sessões abertas sem trabalho e a permitir abrir, retomar e excluir.
**Migration**: substituído por "Entrada "Sessões"".
