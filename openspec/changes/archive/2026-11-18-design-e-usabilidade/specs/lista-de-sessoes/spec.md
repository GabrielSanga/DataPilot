# Spec Delta

## ADDED Requirements

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
"Ir para a próxima", e a tecla F8, SHALL selecionar a próxima demanda da lista, depois da selecionada e voltando ao início, que tenha sessão no status destacado no topo. Uma sessão não vinculada nesse status SHALL levar a "Não vinculadas". Sem sessão no status destacado, a ação SHALL NOT estar disponível.

#### Scenario: Duas demandas pedindo atenção
- **WHEN** `CHG-142` e `CHG-160` têm sessões em "precisa de atenção", `CHG-142` está selecionada e o usuário aciona "Ir para a próxima"
- **THEN** `CHG-160` passa a ser a demanda selecionada

#### Scenario: Fim da lista
- **WHEN** a última demanda com sessão em "precisa de atenção" está selecionada e o usuário pressiona F8
- **THEN** a primeira demanda da lista com sessão nesse status passa a ser a selecionada

#### Scenario: Sessão não vinculada pedindo atenção
- **WHEN** a única sessão em "precisa de atenção" é uma sessão não vinculada
- **THEN** "Ir para a próxima" seleciona "Não vinculadas"

### Requirement: Filtro de demandas
Um campo acima da lista de demandas SHALL filtrar as demandas exibidas pelo texto digitado, comparando com o identificador, o título, a change do OpenSpec e os nomes dos repositórios, sem diferenciar maiúsculas nem acentos. "Não vinculadas" SHALL continuar visível. Ctrl+F SHALL levar o foco ao campo e Esc, no campo, SHALL limpar o filtro. Filtrar SHALL NOT mudar a demanda selecionada.

#### Scenario: Filtro pelo repositório
- **WHEN** o usuário digita "estoque" no filtro
- **THEN** a lista mostra só as demandas que têm o repositório `Estoque` ou esse texto no identificador, no título ou na change, e "Não vinculadas"

#### Scenario: Filtro sem acento
- **WHEN** o usuário digita "relatorio" e existe a demanda "Relatório mensal"
- **THEN** essa demanda aparece na lista

#### Scenario: Limpar o filtro
- **WHEN** o foco está no filtro e o usuário pressiona Esc
- **THEN** o filtro fica vazio e todas as demandas voltam a aparecer

#### Scenario: Demanda selecionada fora do filtro
- **WHEN** o usuário digita um texto que não casa com a demanda selecionada
- **THEN** a área da demanda continua mostrando a demanda selecionada

### Requirement: Nenhuma demanda selecionada
Com nenhuma demanda selecionada, a área da demanda SHALL mostrar só a orientação para selecionar ou criar uma demanda, sem ações, sem progresso e sem textos de uma demanda.

#### Scenario: Painel recém-aberto
- **WHEN** o painel abre e nenhuma demanda está selecionada
- **THEN** a área da demanda mostra só a orientação, sem botões, barra de progresso ou o texto de demanda sem repositórios

### Requirement: Destaque das sessões que precisam do usuário
As linhas de sessões em "precisa de atenção" e "aguardando você" SHALL ter uma faixa na cor do status junto à borda esquerda e o nome do status em destaque. As linhas nos demais status SHALL mostrar o status em texto com um indicador pequeno na cor dele, sem faixa. Uma demanda da lista com sessão em um desses dois status SHALL ter a mesma faixa, na cor do status mais urgente.

#### Scenario: Card com sessões em vários status
- **WHEN** um card tem uma sessão "precisa de atenção", uma "aguardando você" e uma "executando"
- **THEN** as duas primeiras têm faixa vermelha e amarela, e a terceira mostra "executando" com um indicador azul, sem faixa

#### Scenario: Demanda pedindo atenção na lista
- **WHEN** uma demanda tem uma sessão em "precisa de atenção"
- **THEN** o item dela na lista de demandas tem a faixa vermelha

### Requirement: Ação principal e menu de ações
Cada linha e cada bloco SHALL mostrar só a ação principal e reunir as demais num menu "Mais ações": sessão registrada com "Retomar" e, no menu, "Mover para outra demanda" e "Remover registro"; lançamento pendente com "Abrir" e, no menu, "Cancelar"; repositório com "Nova sessão" e, no menu, "Remover repositório"; demanda com "Vincular repositório" e, no menu, "Editar" e "Arquivar" ou "Desarquivar".

#### Scenario: Linha de sessão registrada
- **WHEN** a lista mostra uma sessão registrada
- **THEN** a linha mostra "Retomar" e o botão de mais ações, e "Remover registro" só aparece no menu

#### Scenario: Menu pelo teclado
- **WHEN** o foco está numa linha de sessão registrada e o usuário pressiona Shift+F10
- **THEN** o menu de mais ações da linha abre e pode ser percorrido pelas setas

#### Scenario: Sessão não vinculada
- **WHEN** a lista mostra uma sessão em "Não vinculadas"
- **THEN** a linha mostra só "Vincular", sem menu de mais ações

#### Scenario: Vincular indisponível
- **WHEN** uma sessão não vinculada tem identificador fora do formato GUID
- **THEN** "Vincular" aparece desabilitado e o motivo aparece em texto na própria linha
