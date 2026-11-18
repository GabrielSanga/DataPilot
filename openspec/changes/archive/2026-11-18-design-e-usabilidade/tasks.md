# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a área de trabalho do Windows, configurações do sistema ou um agente interativo.

## 1. Core

- [x] 1.1 Tornar o resumo estruturado (contagens por status, textos com plural correto, contagens da demanda em frase com vírgula, "nenhuma sessão") sem mudar quais sessões entram na contagem; atualizar os testes existentes do resumo e cobrir os cenários de "Texto da interface" sobre contagens, e verificar que passam
- [x] 1.2 Implementar a função pura do filtro de demandas (identificador, título, change e repositórios, sem diferenciar maiúsculas nem acentos, "Não vinculadas" sempre presente); cobrir com testes os cenários de "Filtro de demandas" e verificar que passam
- [x] 1.3 Implementar a função pura que escolhe o status destacado no topo e a próxima demanda para "Ir para a próxima" (atenção antes de aguardando, ordem da lista depois da selecionada, volta ao início, não vinculadas); cobrir com testes os cenários de "Alerta do que precisa do usuário" e "Ir para a próxima sessão que precisa do usuário" e verificar que passam

## 2. Tema

- [x] 2.1 Criar o dicionário `Tema.xaml` com as cores, as fontes, a escala tipográfica e os estilos da decisão 7 do `design.md`, com os pincéis como recurso dinâmico, e mesclá-lo no `App.xaml`; verificar que o painel compila e que os testes continuam passando
- [x] 2.2 Implementar o dicionário de alto contraste, que aponta os mesmos nomes para as cores do sistema, escolhido na abertura e trocado quando o alto contraste muda; verificar por leitura do código que todos os pincéis da janela e dos diálogos vêm de recursos dinâmicos
- [x] 2.3 Aplicar o indicador de foco a todos os controles focáveis e dar nome acessível aos botões só de ícone; verificar por leitura do XAML que nenhum botão de ícone fica sem nome

## 3. Janela principal

- [x] 3.1 Montar a barra superior com o nome do produto, o alerta (vermelho, amarelo ou neutro), "Ir para a próxima" e as contagens em texto, com o esmaecimento único ao acender, desligado quando as animações do Windows estão desligadas
- [x] 3.2 Montar o trilho de demandas com "Nova demanda", o filtro, os itens com identificador em Bahnschrift, contagens em frase e faixa de status, "Não vinculadas" separada no fim e "Mostrar arquivadas" no pé
- [x] 3.3 Corrigir o estado sem seleção, para a área da demanda mostrar só a orientação
- [x] 3.4 Montar a demanda selecionada: identificador e título, change com a barra grafite e "7 de 12 tarefas", "Vincular repositório" e o menu da demanda; repositórios como baias com cabeçalho, linha de 2 px, "Nova sessão" e menu
- [x] 3.5 Montar as faixas de sessão e de lançamento pendente com a ação principal, o menu de mais ações (clique, tecla de menu e Shift+F10), o destaque das sessões que precisam do usuário e o tempo com algarismos de largura fixa; levar "Mostrar antigas" para dentro de "Não vinculadas" e mostrar na linha o motivo de "Vincular" indisponível
- [x] 3.6 Ligar os atalhos Ctrl+N, Ctrl+F, Esc no filtro e F8, e verificar que a demanda selecionada e a rolagem se mantêm nas atualizações da tela
- [x] 3.7 Capturar a janela com os dados de exemplo, como na decisão 11 do `design.md`, sem seleção, com `CHG-142` selecionada e com "Não vinculadas" selecionada; verificar nas capturas o layout da decisão 4, o destaque só das sessões que precisam do usuário e a ausência de texto cortado ou sobreposto, e corrigir o que destoar

## 4. Diálogos, confirmações e erros

- [x] 4.1 Aplicar o tema aos diálogos de nova demanda, editar demanda, nova sessão, vincular repositório, vincular sessão e mover sessão, com botão principal nomeado pela ação, Enter, Esc e erro junto do campo; trocar a escolha do agente por dois botões de escolha
- [x] 4.2 Criar a confirmação do próprio painel, com o botão nomeado pela ação e "Cancelar" como padrão, e usá-la em remover registro, remover repositório, arquivar com sessão em andamento e abrir outra aba de sessão possivelmente aberta
- [x] 4.3 Criar a faixa de erros e mensagens dentro da janela, com botão de fechar, e usá-la no lugar das caixas de mensagem de erro e de informação
- [x] 4.4 Capturar cada diálogo e a faixa de erro com os dados de exemplo; verificar nas capturas o tema, os nomes dos botões e a posição das mensagens de erro

## 5. Documentação

- [x] 5.1 Atualizar o `README.md` com a nova organização da tela, o alerta, "Ir para a próxima", o filtro, os menus de mais ações, os atalhos e o roteiro de verificação manual; verificar que os comandos documentados funcionam como escritos

## 6. Verificação integrada

- [x] 6.1 **[manual]** Usar o painel com sessões reais por um dia; verificar que as sessões que precisam de você são a primeira coisa que se vê e que "Ir para a próxima" leva a elas
- [x] 6.2 **[manual]** Percorrer a janela e todos os diálogos só com o teclado; verificar que o foco é sempre visível e que todos os atalhos e menus funcionam
- [x] 6.3 **[manual]** Ligar o alto contraste do Windows com o painel aberto; verificar que a janela e os diálogos passam a usar as cores do sistema e que os status continuam legíveis
- [x] 6.4 **[manual]** Remover o registro de uma sessão de teste pelo menu; verificar que Enter na confirmação não remove e que "Remover registro" remove

## Workflow follow-up

- Arquivar a `etapa-5a-progresso-openspec` antes ou junto desta change.
- Arquivar esta change depois que as verificações manuais do grupo 6 estiverem concluídas.
