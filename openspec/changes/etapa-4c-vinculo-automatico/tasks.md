# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a conta `ia`, elevação de administrador ou um agente interativo.

## 1. Verificação do hook

- [ ] 1.1 Adicionar em `scripts/testes/verificar.ps1` o caso de "Data de criação do arquivo preservada": gravar um evento, recuar a data de criação do arquivo de status para uma data fixa no passado, gravar outro evento e conferir que a data de criação continua a fixada; verificar que todos os casos passam sem alterar o `hook.ps1`

## 2. Core

- [ ] 2.1 Levar ao modelo de sessão lida a data de criação do arquivo de status, obtida do sistema de arquivos na leitura da pasta; cobrir com testes os cenários de "Data de criação do arquivo de status" e verificar que passam
- [ ] 2.2 Implementar em `DadosPainel` os lançamentos pendentes por repositório: registrar, desfazer, carregar descartando os inválidos, remover os de mais de 24 horas ao gravar e preservar campos desconhecidos; cobrir com testes os cenários de "Registro de lançamentos pendentes" e verificar que passam
- [ ] 2.3 Fazer a abertura de sessão nova do Codex registrar o lançamento, com o rótulo, antes de iniciar o terminal e desfazê-lo se o terminal não abrir, sem gerar lançamento para o Claude nem para "Retomar"; cobrir com testes os cenários de "Registro das sessões abertas pelo painel" e verificar que passam
- [ ] 2.4 Implementar a função pura de correspondência entre sessões não registradas e lançamentos pendentes, com os critérios, os limites e a escolha do lançamento mais recente; cobrir com testes os cenários de "Vínculo automático por lançamento pendente", "Limites do vínculo automático" e "Escolha entre lançamentos pendentes" e verificar que passam
- [ ] 2.5 Implementar em `DadosPainel` a operação que aplica um vínculo por lançamento numa única gravação: recusar se o lançamento não existir mais ou se o identificador já estiver registrado, registrar a sessão com o rótulo do lançamento e `vinculadaEm`, e remover o lançamento; cobrir com testes "Lançamento usado", "Vínculo por lançamento", "Conteúdo gravado no vínculo por lançamento", "Sessão do Codex vinculada por lançamento" e "Registro removido não é refeito", e verificar que passam

## 3. Aplicação no painel

- [ ] 3.1 Aplicar os vínculos automáticos a cada retrato da pasta de status, antes de montar a tela, ignorando falhas de gravação; cobrir com teste a aplicação por duas instâncias sobre o mesmo arquivo e verificar que a sessão fica registrada uma única vez
- [ ] 3.2 Fazer o gerador de exemplos produzir um arquivo de dados com um lançamento pendente anterior à criação de uma sessão de exemplo do Codex; verificar no painel, com a pasta de exemplo, que a sessão aparece direto no card com o rótulo, mantendo a demanda selecionada, e que "Remover" a devolve a "Não vinculadas" sem que ela volte sozinha
- [ ] 3.3 Atualizar o `README.md` com o vínculo automático do Codex, os limites, os casos que continuam manuais e o roteiro de verificação manual; verificar que os comandos documentados funcionam como escritos

## 4. Verificação integrada

- [ ] 4.1 **[manual]** Copiar o `verificar.ps1` novo para `C:\Painel` como administrador e rodá-lo como `ia`; verificar que todos os casos passam
- [ ] 4.2 **[manual]** Abrir uma sessão nova do Codex pelo card de um repositório, com rótulo, e enviar uma mensagem; verificar que ela aparece no card dessa demanda, com o rótulo e o status, sem passar por "Não vinculadas", e que "Retomar" abre a conversa certa
- [ ] 4.3 **[manual]** Com o mesmo repositório em duas demandas, abrir o Codex por uma delas e enviar uma mensagem; verificar que a sessão entra na demanda em que o clique foi feito
- [ ] 4.4 **[manual]** Com uma sessão antiga do Codex em "Não vinculadas" na mesma pasta, abrir uma sessão nova pelo card e enviar uma mensagem na antiga; verificar que a antiga continua em "Não vinculadas"
- [ ] 4.5 **[manual]** Remover o registro de uma sessão vinculada automaticamente; verificar que ela volta a "Não vinculadas" e não é vinculada de novo sozinha
- [ ] 4.6 **[manual]** Abrir uma sessão do Codex à mão, fora do painel, numa pasta sem lançamento pendente; verificar que ela aparece em "Não vinculadas" e pode ser vinculada manualmente

## Workflow follow-up

- Repetir o roteiro manual depois de atualizar o Codex.
- Arquivar a change depois que as verificações manuais do grupo 4 estiverem concluídas.
