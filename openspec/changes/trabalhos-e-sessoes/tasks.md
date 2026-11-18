# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a área de trabalho do Windows, a conta `ia` ou um agente interativo.

## 1. Dados (Core)

- [ ] 1.1 Gerar o identificador da demanda (`t` + 8 dígitos hexadecimais, sem colisão) e criar e editar demanda só com o título; cobrir com testes os cenários de "Criar trabalho pelo título" e "Editar o título do trabalho" e verificar que passam
- [ ] 1.2 Levar `openspecChange` para o repositório: gravação, validação, herança da change da demanda na carga e preservação do campo antigo; cobrir com testes os cenários de "Change do OpenSpec no repositório", "Campos da demanda" e "Validação das demandas ao carregar" e verificar que passam
- [ ] 1.3 Implementar a lista `excluidas` e a operação de excluir (remoção do registro e inclusão na lista numa só gravação, identificador validado pela regra do hook, exclusão repetida sem efeito); cobrir com testes os cenários de "Sessões excluídas", "Excluir sessão" e "Dados de status nunca são gravados" e verificar que passam
- [ ] 1.4 Implementar a listagem das changes ativas de um repositório (sem `archive`, sem atalho nem junção em `openspec`, `changes` e nas entradas, nomes válidos, até 200, em ordem) e a validação da pasta escolhida (filha direta da raiz, nome válido); cobrir com testes os cenários de "Vincular repositório pela pasta" e "Change do repositório", com junções criadas por `mklink /J` numa pasta temporária, e verificar que passam

## 2. Visão, vínculo e lançamento (Core)

- [ ] 2.1 Montar a entrada "Sessões" na visão (registradas da `sem-demanda` com o repositório, lançamentos dela e não registradas não excluídas) e tirar a `sem-demanda` da lista de trabalhos; fazer a visão, o resumo, o alerta, "Ir para a próxima" e a correspondência automática ignorarem as excluídas; cobrir com testes os cenários de "Entrada "Sessões"", "Ir para a próxima sessão que precisa do usuário" e "Registro removido não é refeito" e verificar que passam
- [ ] 2.2 Implementar "Retomar" de sessão não registrada (registro na `sem-demanda` no repositório escolhido, com `vinculadaEm`, só GUID canônico) e o vínculo a um trabalho movendo uma sessão registrada da `sem-demanda`; cobrir com testes os cenários de "Retomar sessão não registrada", "Vincular sessão não vinculada" e "Sessão única no arquivo" e verificar que passam
- [ ] 2.3 Implementar "Nova sessão" em "Sessões" (vincular a pasta à `sem-demanda` se preciso e seguir o fluxo de sempre) e o novo título da aba (título do trabalho com até 40 caracteres e `;` trocado por `,`); atualizar os testes do montador e cobrir os cenários de "Nova sessão" e "Título da aba e rótulo", e verificar que passam
- [ ] 2.4 Fazer o progresso usar a change de cada repositório, sem soma no trabalho, com as indicações por card; atualizar os testes do progresso e cobrir os cenários de "Localização do tasks.md", "Exibição do progresso" e "Atualização do progresso", e verificar que passam
- [ ] 2.5 Ajustar o filtro para comparar título, repositórios e changes dos repositórios, sem o identificador; cobrir com testes os cenários de "Filtro de demandas" e verificar que passam

## 3. Tela

- [ ] 3.1 Trocar "demanda" por "trabalho" e "Não vinculadas" por "Sessões" em todos os textos da janela, dos diálogos, das confirmações e das mensagens; verificar com uma busca nos arquivos da interface que nenhum texto exibido usa os termos antigos
- [ ] 3.2 Refazer o diálogo de trabalho (só título) e o de nova sessão (só agente e rótulo), e tirar o texto de ajuda permanente de todos os diálogos, mantendo os erros junto dos campos
- [ ] 3.3 Refazer "Vincular repositório" com a escolha de pasta do Windows na raiz dos fontes, o erro para pasta fora da raiz e a lista de changes com "Nenhuma"; criar "Trocar change" no menu do repositório com a mesma lista
- [ ] 3.4 Mostrar no card de cada repositório o nome da change, a barra e o progresso, e tirar a change e a soma do topo do trabalho
- [ ] 3.5 Refazer a linha da sessão: indicador de status à esquerda do agente, rótulo em branco quando não há, status na dica e no nome acessível, status em texto com alto contraste, "Retomar" e o botão "Excluir" com ícone e nome acessível; tirar "Mover" e "Remover registro" das sessões de trabalho
- [ ] 3.6 Montar a área "Sessões" com "Nova sessão", o repositório em cada linha, "Retomar" com escolha do repositório para as não registradas, "Excluir" e o menu com "Vincular a um trabalho"
- [ ] 3.7 Usar a confirmação do painel para excluir, com "Excluir sessão" e "Cancelar" como padrão
- [ ] 3.8 Atualizar o gerador de exemplos (trabalhos sem identificador visível, changes por repositório, sessões em "Sessões", uma sessão excluída) e capturar a janela com os dados de exemplo, como na change de design: sem seleção, com um trabalho selecionado, com "Sessões" selecionada e cada diálogo; verificar nas capturas cada item desta change e a ausência de texto cortado ou sobreposto

## 4. Documentação

- [ ] 4.1 Atualizar o `README.md` com "Trabalho", "Sessões", vincular repositório pela pasta, change por repositório, excluir sessão e o roteiro de verificação manual; verificar que os comandos documentados funcionam como escritos

## 5. Verificação integrada

- [ ] 5.1 **[manual]** Criar um trabalho só com o título, vincular um repositório de `C:\Fontes` pela escolha de pasta e escolher uma change; verificar o progresso no card e que nenhum identificador aparece
- [ ] 5.2 **[manual]** Tentar vincular uma pasta fora de `C:\Fontes`; verificar o erro e que nada é vinculado
- [ ] 5.3 **[manual]** Em "Sessões", abrir uma sessão nova do Claude e uma do Codex; verificar que aparecem lá e que "Retomar" abre a conversa certa. Abrir uma sessão à mão, pelo SSH, e retomá-la por "Sessões"
- [ ] 5.4 **[manual]** Excluir uma sessão de um trabalho e uma de "Sessões"; verificar que somem do painel, inclusive depois de reabrir o painel, e que Enter na confirmação não exclui
- [ ] 5.5 **[manual]** Abrir uma sessão por um trabalho com título contendo `;`; verificar o título da aba

## Workflow follow-up

- Arquivar a change depois que as verificações manuais do grupo 5 estiverem concluídas.
- Rever a `etapa-5b-bandeja-e-avisos` contra esta change antes de implementá-la (textos das notificações com "trabalho" e "Sessões", e sessões excluídas fora das notificações).
