# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem a área de trabalho do Windows ou um agente interativo.

## 1. Core

- [ ] 1.1 Implementar a função pura que, a cada retrato, compara o status exibido de cada sessão com o anterior e decide o que notificar: entrada em "precisa de atenção", 2 minutos em "aguardando você", descarte com o painel em foco, abertura sem notificação, repetição dentro de 2 minutos e sessão que vira inativa; cobrir com testes os cenários de "Notificação de sessão que precisa do usuário" e "Limites das notificações", com relógio controlado, e verificar que passam
- [ ] 1.2 Montar o texto da notificação (uma sessão ou várias, até três listadas) só com dados do arquivo de dados, e o alvo do clique (demanda ou "Não vinculadas"); cobrir com testes os cenários de "Texto da notificação vem do arquivo de dados" e verificar que passam
- [ ] 1.3 Expor a contagem de sessões em "precisa de atenção" usada pelo resumo do cabeçalho; cobrir com teste que as duas contagens coincidem, inclusive com demandas arquivadas ocultas, e verificar que passa

## 2. Painel

- [ ] 2.1 Habilitar o Windows Forms no projeto WPF sem o `using` global e criar, num único arquivo, o ícone da bandeja desenhado em tempo de execução (neutro, número de 1 a 9 e "9+"), com a dica do resumo e o menu "Abrir painel" e "Sair"; remover o ícone na saída; verificar que o painel compila e que os testes continuam passando
- [ ] 2.2 Ligar a função de decisão ao ciclo da tela, informando se a janela está ativa, e mostrar a notificação; ao clicar nela ou no ícone, restaurar e ativar a janela e selecionar a demanda; verificar com a pasta de exemplo, alterando um arquivo de status com o painel minimizado, que a notificação aparece e que o clique seleciona a demanda certa
- [ ] 2.3 Atualizar o `README.md` com o ícone, as notificações, as regras de quando notificar e o roteiro de verificação manual; verificar que os comandos documentados funcionam como escritos

## 3. Verificação integrada

- [ ] 3.1 **[manual]** Abrir o painel com uma sessão já em "precisa de atenção"; verificar que o ícone mostra a contagem e que nenhuma notificação aparece
- [ ] 3.2 **[manual]** Com o painel minimizado, provocar um pedido de permissão numa sessão do Claude; verificar que a notificação aparece com a identificação da sessão e que o clique traz o painel com a demanda selecionada, sem abrir aba
- [ ] 3.3 **[manual]** Trabalhar numa aba respondendo ao agente em menos de 2 minutos; verificar que nenhuma notificação de "aguardando você" aparece. Depois deixar uma sessão esperando por mais de 2 minutos com o painel fora de foco e verificar que a notificação aparece uma vez
- [ ] 3.4 **[manual]** Fechar a janela do painel; verificar que o painel encerra e que o ícone some da bandeja

## Workflow follow-up

- Arquivar a change depois que as verificações manuais do grupo 3 estiverem concluídas.
