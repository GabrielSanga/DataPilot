# Proposal

## Why

O painel mostra o status das sessões, mas só para quem está olhando para ele. Com o trabalho acontecendo nas abas do Windows Terminal, uma sessão que pede permissão ou que terminou e espera resposta passa despercebida até alguém voltar ao painel. O PRD prevê um ícone na bandeja com o número de sessões que precisam de atenção e uma notificação do Windows quando uma sessão entra em "precisa de atenção" ou "aguardando você", inclusive com o painel minimizado.

## What Changes

- Enquanto o painel está aberto, um ícone na área de notificação do Windows mostra quantas sessões estão em "precisa de atenção"; a dica do ícone traz o resumo do cabeçalho.
- Clicar no ícone traz a janela para a frente; o menu do ícone tem "Abrir painel" e "Sair".
- Fechar a janela encerra o painel e remove o ícone, como hoje. Minimizar mantém o ícone e os avisos.
- O painel mostra uma notificação do Windows quando uma sessão entra em "precisa de atenção", e quando fica em "aguardando você" por pelo menos 2 minutos, sempre que a janela do painel não estiver em primeiro plano.
- Nada é notificado na abertura do painel; várias mudanças no mesmo ciclo viram uma notificação só; uma mesma sessão não é notificada de novo pelo mesmo status em menos de 2 minutos.
- O texto da notificação vem do arquivo de dados (demanda, repositório, rótulo, agente); sessões não vinculadas aparecem só pelo agente. Clicar na notificação traz a janela e seleciona a demanda da sessão.

Fora desta change: o progresso do OpenSpec (change `etapa-5a-progresso-openspec`), som, configuração dos avisos pela tela e a opção de manter o painel na bandeja ao fechar.

## Capabilities

### New Capabilities

- `avisos`: ícone na bandeja com a contagem de sessões que precisam de atenção e notificações do Windows quando uma sessão passa a precisar do usuário.

### Modified Capabilities

Nenhuma.

## Impact

- **Código:** uma função do `DataPilot.Core` que decide, a cada retrato, o que notificar; no `DataPilot.Wpf`, o ícone da bandeja, as notificações e a ligação com a janela.
- **Dependências:** nenhum pacote novo. O ícone e as notificações usam os recursos de bandeja que acompanham o .NET para Windows, em vez do pacote de terceiros sugerido no PRD.
- **Segurança:** o ícone e as notificações só mostram informação e trazem a janela para a frente; nunca executam ação. O texto vem do arquivo de dados, não do arquivo de status.
- **Scripts e dados:** nenhuma mudança.
