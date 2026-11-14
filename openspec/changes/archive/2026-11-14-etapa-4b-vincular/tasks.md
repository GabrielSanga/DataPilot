# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem olhar a janela ou usar agentes reais na conta `ia`.

## 1. Pedido de vínculo e gravação

- [x] 1.1 Criar no Core o tipo do pedido de vínculo, só com agente e identificador, e a função que o cria a partir de uma sessão lida, recusando identificador que não seja GUID canônico; cobrir com testes os cenários de "Somente GUID canônico pode ser vinculado" e verificar que passam
- [x] 1.2 Implementar em `DadosPainel` a operação de vínculo (pedido, demanda, pasta, rótulo e data), gravando `vinculadaEm` e vinculando o repositório à demanda quando faltar; cobrir com testes os cenários de "Vincular sessão não vinculada", "Rastro do vínculo" e "Sessão já registrada não é vinculada de novo", mais demanda arquivada, demanda inexistente e arquivo ilegível, e verificar que passam
- [x] 1.3 Adicionar o teste que vincula uma sessão cuja pasta de trabalho tem um texto reconhecível e confere que o arquivo de dados não contém esse texto nem status, evento ou data do arquivo de status; verificar que passa e que os cenários de "Somente identificador e agente cruzam" estão cobertos
- [x] 1.4 Verificar por teste que uma sessão nova aberta pelo painel não recebe `vinculadaEm` e que remover o registro de uma sessão vinculada a devolve à lista de não vinculadas na visão

## 2. Regras de tela

- [x] 2.2 Implementar a função pura que decide o aviso de pasta divergente e a que informa se uma sessão não vinculada pode ser vinculada, com o motivo; cobrir com testes os cenários de "Aviso de pasta divergente" e verificar que passam

## 3. Interface

- [x] 3.1 Adicionar "Vincular" às linhas de "Não vinculadas", com o diálogo de demanda, repositório e rótulo, a pré-seleção por coincidência de nome, a orientação quando não há demanda e o aviso de pasta divergente; verificar com `--dados` e a pasta de exemplo que o vínculo reflete no arquivo e que a sessão passa para o card
- [x] 3.3 Desabilitar a ação para identificador fora do formato, com a dica do motivo, e tratar a recusa de identificador já registrado; verificar com um arquivo de status de exemplo cujo identificador não é GUID
- [x] 3.4 Atualizar o `README.md` com o vínculo e o roteiro de verificação manual, e os dados de exemplo com uma sessão não vinculada do Codex; verificar que os comandos documentados funcionam como escritos

## 4. Verificação integrada

- [x] 4.1 **[manual]** Abrir uma sessão nova do Codex pelo card de um repositório; verificar que ela aparece em "Não vinculadas" e não no card, vinculá-la pelo diálogo com a demanda e o repositório pré-selecionados, e conferir que passa a mostrar status na demanda
- [x] 4.2 **[manual]** Encerrar essa sessão do Codex e acionar "Retomar"; verificar que a conversa certa abre
- [x] 4.3 **[manual]** Numa sessão do Claude registrada, executar `/clear`; vincular a sessão nova pelo diálogo de "Não vinculadas" e verificar que ela pode ser retomada
- [x] 4.4 **[manual]** Vincular uma sessão a um repositório diferente da pasta dela; verificar o aviso, cancelar, e depois remover o registro de uma sessão vinculada e conferir que ela volta a "Não vinculadas"
- [x] 4.5 **[manual]** Abrir o `demandas.json` depois dos vínculos; verificar que as sessões vinculadas têm `vinculadaEm` e que nenhuma pasta de trabalho do arquivo de status aparece no arquivo

## 5. Remoção das sugestões no card

Este grupo retira um recurso que chegou a ser implementado e deve ser concluído antes das verificações do grupo 4.

- [x] 5.1 Remover da visão agrupada as sugestões por card e os testes delas, e acrescentar um teste de que uma sessão não vinculada de mesma pasta não aparece em nenhum card; verificar que a solution compila e que todos os testes passam
- [x] 5.2 Remover dos cards a área de sugestões e a ação "Vincular aqui", com o código de interface que só servia a elas, mantendo a pré-seleção do diálogo de "Não vinculadas"; verificar com a pasta de exemplo que o card mostra só sessões registradas e que o diálogo ainda vem pré-selecionado
- [x] 5.3 Retirar as sugestões do `README.md` e do roteiro de verificação manual; verificar que o texto descreve só o vínculo por "Não vinculadas"

## Workflow follow-up

- Planejar a change do vínculo automático por identificador de lançamento, começando pelo teste de variável de ambiente nos hooks do Codex.
- Arquivar a change depois que as verificações manuais do grupo 4 estiverem concluídas.
