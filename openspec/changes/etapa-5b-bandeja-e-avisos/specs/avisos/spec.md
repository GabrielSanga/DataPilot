# Spec Delta

## Purpose

Avisa o usuário, pela bandeja do Windows e por notificações, quando uma sessão precisa dele, mesmo com o painel minimizado ou fora de foco, sem gerar uma notificação a cada resposta do agente e sem que o ícone ou a notificação executem qualquer ação além de mostrar o painel.

## ADDED Requirements

### Requirement: Ícone na bandeja
Enquanto o painel estiver aberto, SHALL haver um ícone na área de notificação do Windows que mostra a quantidade de sessões em "precisa de atenção", a mesma do resumo do cabeçalho, de 1 a 9 ou "9+", e um ícone neutro quando não houver nenhuma. A dica do ícone SHALL trazer o resumo do cabeçalho.

#### Scenario: Duas sessões precisam de atenção
- **WHEN** duas sessões estão em "precisa de atenção"
- **THEN** o ícone mostra o número 2 e a dica mostra o resumo do cabeçalho

#### Scenario: Nenhuma sessão precisa de atenção
- **WHEN** nenhuma sessão está em "precisa de atenção"
- **THEN** o ícone aparece sem número

#### Scenario: Mais de nove sessões
- **WHEN** dez sessões estão em "precisa de atenção"
- **THEN** o ícone mostra "9+"

### Requirement: Ícone traz a janela
Clicar no ícone, ou em "Abrir painel" no menu dele, SHALL restaurar e ativar a janela do painel. "Sair", no menu do ícone, SHALL encerrar o painel.

#### Scenario: Painel minimizado
- **WHEN** o painel está minimizado e o usuário clica no ícone
- **THEN** a janela volta ao tamanho anterior, em primeiro plano

#### Scenario: Sair pelo menu
- **WHEN** o usuário escolhe "Sair" no menu do ícone
- **THEN** o painel encerra e o ícone some

### Requirement: Fechar encerra o painel
Fechar a janela SHALL encerrar o painel e remover o ícone da bandeja. Minimizar a janela SHALL manter o ícone e as notificações.

#### Scenario: Fechar a janela
- **WHEN** o usuário fecha a janela do painel
- **THEN** o painel encerra e o ícone deixa de aparecer na bandeja

#### Scenario: Minimizar a janela
- **WHEN** o usuário minimiza a janela e uma sessão entra em "precisa de atenção"
- **THEN** o ícone mostra a contagem nova e a notificação aparece

### Requirement: Notificação de sessão que precisa do usuário
Com a janela do painel fora de foco, o painel SHALL mostrar uma notificação do Windows quando uma sessão entrar em "precisa de atenção" e quando uma sessão estiver em "aguardando você" há pelo menos 2 minutos sem ter saído desse status. Com a janela em foco, SHALL NOT notificar.

#### Scenario: Pedido de permissão com o painel minimizado
- **WHEN** o painel está minimizado e uma sessão passa para "precisa de atenção"
- **THEN** aparece uma notificação "Precisa de atenção" com a identificação da sessão

#### Scenario: Resposta respondida logo
- **WHEN** uma sessão fica "aguardando você" e o usuário escreve na aba em menos de 2 minutos
- **THEN** nenhuma notificação aparece

#### Scenario: Agente terminou e o usuário saiu
- **WHEN** uma sessão está "aguardando você" há 2 minutos e o painel está fora de foco
- **THEN** aparece uma notificação "Aguardando você" com a identificação da sessão

#### Scenario: Painel em foco
- **WHEN** a janela do painel está ativa e uma sessão passa para "precisa de atenção"
- **THEN** nenhuma notificação aparece, e a tela e o ícone mostram o novo status

### Requirement: Limites das notificações
O painel SHALL NOT notificar na abertura sessões que já estejam em "precisa de atenção" ou "aguardando você", SHALL NOT notificar a mesma sessão pelo mesmo status mais de uma vez em 2 minutos, e SHALL reunir numa única notificação as que surgirem na mesma atualização da tela.

#### Scenario: Abertura do painel
- **WHEN** o painel abre com uma sessão já em "precisa de atenção"
- **THEN** nenhuma notificação aparece e o ícone mostra a contagem

#### Scenario: Status alternando
- **WHEN** uma sessão entra em "precisa de atenção", sai e entra de novo dentro de 2 minutos
- **THEN** só a primeira entrada é notificada

#### Scenario: Várias sessões ao mesmo tempo
- **WHEN** três sessões passam a precisar do usuário na mesma atualização da tela
- **THEN** aparece uma única notificação, que diz quantas são e lista as sessões

### Requirement: Texto da notificação vem do arquivo de dados
A notificação SHALL identificar a sessão só com dados do arquivo de dados: o `id` da demanda, o repositório e o rótulo, no formato do título da aba, e o agente. Uma sessão não vinculada SHALL ser identificada só pelo agente. Nenhum texto do arquivo de status SHALL aparecer na notificação.

#### Scenario: Sessão vinculada
- **WHEN** a sessão do Claude registrada em `Faturamento` da demanda `CHG-142`, com o rótulo `ajuste`, passa a precisar de atenção
- **THEN** a notificação identifica a sessão como `CHG-142 · Faturamento · ajuste`, do Claude

#### Scenario: Sessão não vinculada
- **WHEN** uma sessão do Codex não vinculada passa a precisar de atenção
- **THEN** a notificação diz "Sessão do Codex não vinculada" e não mostra a pasta de trabalho

### Requirement: Clicar na notificação
Clicar na notificação SHALL restaurar e ativar a janela do painel e selecionar a demanda da sessão notificada, ou "Não vinculadas", e SHALL NOT abrir, retomar nem alterar nenhuma sessão.

#### Scenario: Notificação de uma sessão vinculada
- **WHEN** o usuário clica na notificação de uma sessão de `CHG-142`
- **THEN** a janela aparece com `CHG-142` selecionada e nenhuma aba é aberta

#### Scenario: Notificação de várias sessões
- **WHEN** o usuário clica numa notificação que reúne sessões de duas demandas
- **THEN** a janela aparece com a demanda da primeira sessão listada selecionada
