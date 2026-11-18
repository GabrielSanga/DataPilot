# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a conta `ia`, elevação de administrador ou um agente interativo.

## 1. Ensaio da retomada do Claude

- [x] 1.1 **[manual]** Executar como `ia` o ensaio da decisão 3 do `design.md`; verificar que a retomada de uma sessão sem mensagem termina com código diferente de 0 e que `claude --session-id` com o mesmo identificador abre normalmente. Se um dos dois não se confirmar, parar e rever a decisão 1 do design antes de seguir

## 2. Scripts

- [x] 2.1 Fazer o `abrir.ps1`, na retomada do Claude, medir a duração e, se o código for diferente de 0 em menos de 30 segundos, avisar na aba e iniciar `claude --session-id` com o mesmo identificador, terminando com o código da segunda execução; manter o arquivo só em ASCII e sem mudar a retomada do Codex
- [x] 2.2 Adicionar em `verificar.ps1` os casos da retomada com um `claude` e um `codex` falsos à frente no `PATH`: sucesso, falha imediata, falha depois de 30 segundos e falha do Codex, conferindo os argumentos recebidos e o código final; pular esses casos com aviso quando não houver pasta válida em `C:\Fontes`; verificar que todos os casos passam
- [x] 2.3 Atualizar o guia de instalação com a recópia do `abrir.ps1` e do `verificar.ps1` e com a descrição da retomada sem mensagem; verificar que os comandos do guia estão completos

## 3. Core

- [x] 3.1 Levar à visão agrupada os lançamentos pendentes válidos de cada card, do mais recente para o mais antigo e com o tempo decorrido, sem os vencidos nem os de demanda arquivada, e sem alterar as contagens, o status mais urgente, a ordem das demandas e o resumo do cabeçalho; cobrir com testes os cenários de "Lançamento pendente no card" e "Lançamento pendente fora das contagens" e verificar que passam
- [x] 3.2 Expor no `DadosPainel` o cancelamento de um lançamento pendente, reaproveitando a operação de desfazer, sem alterar o arquivo quando o lançamento não existir; cobrir com testes os cenários de "Cancelar lançamento pendente", incluindo a sessão que surge depois e não é vinculada, e verificar que passam

## 4. Painel

- [x] 4.1 Mostrar no card a linha de cada lançamento pendente, com o agente, o rótulo, "aguardando a primeira mensagem" e o tempo decorrido, e a ação "Cancelar"; sincronizar as linhas por identificador do lançamento; verificar com a pasta de exemplo que a demanda selecionada e a posição da lista se mantêm quando a linha surge, some ou é substituída pela sessão
- [x] 4.2 Fazer o gerador de exemplos produzir um lançamento pendente sem sessão correspondente; verificar no painel, com a pasta de exemplo, que a linha aparece, que "Cancelar" a remove e que o resumo da demanda não conta o lançamento
- [x] 4.3 Atualizar o `README.md` com a linha do lançamento pendente, a ação "Cancelar", a retomada de sessão do Claude sem mensagem e o roteiro de verificação manual; verificar que os comandos documentados funcionam como escritos

## 4a. Abrir de novo um lançamento pendente

- [x] 4a.1 Implementar no Core a reabertura de um lançamento pendente: montar o comando de sessão nova com o repositório e o rótulo do lançamento, renovar a data antes de iniciar o terminal e restaurá-la se ele não abrir, recusando lançamento vencido, de demanda arquivada ou inexistente; cobrir com testes os cenários de "Abrir de novo um lançamento pendente" e verificar que passam
- [x] 4a.2 Acrescentar a ação "Abrir" à linha do lançamento no card e atualizar o `README.md`; verificar que o painel compila e que os testes continuam passando

## 5. Verificação integrada

- [x] 5.1 **[manual]** Copiar o `abrir.ps1` e o `verificar.ps1` novos para `C:\Painel` como administrador e rodar a verificação como `ia`; verificar que todos os casos passam
- [x] 5.2 **[manual]** Abrir uma sessão nova do Claude pelo card, fechar a aba sem enviar mensagem e acionar "Retomar"; verificar que o Claude abre, que o status volta a "ativa" e que a mesma linha do card continua valendo
- [x] 5.3 **[manual]** Numa sessão do Claude com mensagens, fechar a aba e acionar "Retomar"; verificar que a conversa volta com o histórico
- [x] 5.4 **[manual]** Abrir uma sessão nova do Codex pelo card; verificar que a linha "aguardando a primeira mensagem" aparece em seguida e que, ao enviar a primeira mensagem, ela é substituída pela sessão, com a demanda selecionada mantida
- [x] 5.5 **[manual]** Abrir uma sessão nova do Codex, fechar a aba sem mensagem e acionar "Cancelar"; verificar que a linha some e que um Codex aberto depois à mão na mesma pasta aparece em "Não vinculadas"
- [x] 5.6 **[manual]** Abrir uma sessão nova do Codex, fechar a aba sem mensagem e acionar "Abrir" na linha; verificar que o Codex abre no mesmo repositório, que o card continua com uma linha só e que, ao enviar a primeira mensagem, a sessão entra no card com o rótulo

## Workflow follow-up

- Repetir o ensaio da tarefa 1.1 depois de atualizar o Claude Code.
- Arquivar a etapa 4c antes desta change.
- Arquivar esta change depois que as verificações manuais do grupo 5 estiverem concluídas.
