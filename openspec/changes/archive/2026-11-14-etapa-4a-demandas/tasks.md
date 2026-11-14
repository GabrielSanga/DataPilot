# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem olhar a janela ou usar agentes reais na conta `ia`.

## 1. Modelo e validação

- [x] 1.1 Acrescentar ao validador único as regras de `id`, `titulo` e `openspecChange` da demanda; cobrir com testes os cenários de "Criar demanda" (limites de 30 e 80 caracteres, só pontos, `;`, espaço, caractere de controle no título) e verificar que passam
- [x] 1.2 Criar o tipo de demanda no Core, incluir o `id` da demanda na sessão registrada e trocar o estado carregado para uma lista de demandas, ajustando os usos existentes; verificar que a solution compila e que os testes existentes continuam passando

## 2. Arquivo de dados com várias demandas

- [x] 2.1 Alterar a carga para várias demandas: deduplicar pasta dentro da demanda, deduplicar identificador de sessão no arquivo, desconsiderar demanda inválida ou de `id` repetido e tratar `openspecChange` inválido como ausente; cobrir com testes os cenários de "Validação ao carregar", "Campos da demanda" e "Sessão única no arquivo", incluindo um arquivo no formato gravado pela Etapa 3, e verificar que passam
- [x] 2.2 Implementar criar, editar, arquivar e desarquivar demanda; cobrir com testes os cenários de "Criar demanda", "Editar demanda" e "Arquivar e desarquivar" que dependem só dos dados, e a recusa de editar ou arquivar `sem-demanda`, e verificar que passam
- [x] 2.3 Passar o vínculo e a remoção de repositório a operar por demanda; cobrir com testes os cenários de "Cadastro de repositórios" e "Remoção de repositório", incluindo a mesma pasta em duas demandas, e verificar que passam
- [x] 2.4 Passar o registro de sessão a operar por demanda e implementar mover e remover sessão; cobrir com testes os cenários de "Mover sessão para outra demanda" e "Remover o registro de uma sessão" e verificar que passam
- [x] 2.5 Adicionar testes de que as operações novas preservam campos desconhecidos e não gravam em arquivo ilegível; verificar que passam

## 3. Lançamento com demanda

- [x] 3.1 Passar o `id` da demanda ao montador e à operação de nova sessão, com o título `id · pasta [· rótulo]` e o nome do agente para `sem-demanda`; atualizar e ampliar os testes do montador com os cenários de "Título da aba e rótulo" e verificar que a lista dos demais argumentos não mudou
- [x] 3.2 Fazer a nova sessão do Claude registrar no repositório da demanda indicada; cobrir com testes o cenário de "Nova sessão" e o de mesma pasta em duas demandas, e verificar que passam

## 4. Visão agrupada

- [x] 4.1 Acrescentar o status exibido "sem status" e implementar a função pura que monta a visão agrupada (demandas, cards, sessões registradas com ou sem arquivo de status, não vinculadas); cobrir com testes os cenários de "Tela agrupada por demanda", "Sessões registradas sempre visíveis na demanda" e "Entrada Não vinculadas" e verificar que passam
- [x] 4.2 Implementar o resumo e a ordem das demandas e a ordem das sessões dentro dos cards; cobrir com testes os cenários de "Resumo e ordem das demandas" e "Ordem por urgência", incluindo demanda sem sessões e demanda arquivada, e verificar que passam
- [x] 4.3 Restringir o filtro de antigas às não vinculadas e manter o resumo do cabeçalho; cobrir com testes os cenários de "Sessões antigas ocultas por padrão" e que "sem status" não entra no resumo, e verificar que passam

## 5. Interface

- [x] 5.1 Reorganizar a janela principal em lista de demandas e demanda selecionada, com view models atualizados no lugar por chave; verificar com dados de exemplo que a demanda selecionada se mantém quando um arquivo de status muda e quando a ordem das demandas muda
- [x] 5.2 Adicionar os diálogos de demanda (criar e editar) e as ações de arquivar, desarquivar e "Mostrar arquivadas", com as mensagens de recusa e o aviso de sessão em andamento; verificar com `--dados` num arquivo temporário que cada ação reflete no arquivo
- [x] 5.3 Adicionar "Vincular repositório" com sugestões, a remoção de repositório por demanda e "Nova sessão" no card, e remover a janela "Repositórios"; verificar com `--dados` que o mesmo repositório pode ser vinculado a duas demandas e que a sessão nova fica na demanda certa
- [x] 5.4 Adicionar "Mover" e "Remover" na linha da sessão, a exibição de "sem status" e a entrada "Não vinculadas" com a opção de mostrar antigas; verificar com dados de exemplo os estados de demanda sem repositórios e de nenhuma demanda
- [x] 5.5 Ampliar os dados de exemplo com um arquivo de dados de várias demandas e atualizar o `README.md` com a tela nova, as ações e o roteiro de verificação manual; verificar que os comandos documentados funcionam como escritos

## 6. Verificação integrada

- [x] 6.1 **[manual]** Copiar o `demandas.json` real, abrir o painel novo e verificar que "Sem demanda" aparece com os repositórios e as sessões de antes, e que "Retomar" continua abrindo a conversa certa
- [x] 6.2 **[manual]** Criar uma demanda, vincular dois repositórios e abrir uma sessão do Claude em cada um; verificar que aparecem agrupadas, que o título de cada aba começa pelo `id` da demanda e que o resumo na lista acompanha o status
- [x] 6.3 **[manual]** Mover uma sessão de "Sem demanda" para a demanda criada e retomá-la; verificar que a conversa certa abre
- [x] 6.4 **[manual]** Abrir uma sessão do Codex pelo card e executar `/clear` numa sessão do Claude; verificar que as duas sessões novas aparecem em "Não vinculadas", sem ações
- [x] 6.5 **[manual]** Com uma demanda selecionada e sessões em andamento, usar o painel por alguns minutos; verificar que a seleção e a rolagem não mudam sozinhas, e depois arquivar e desarquivar a demanda

## Workflow follow-up

- Levar para a segunda change da Etapa 4 o que o uso mostrar sobre a entrada "Não vinculadas".
- Arquivar a change depois que as verificações manuais do grupo 6 estiverem concluídas.
