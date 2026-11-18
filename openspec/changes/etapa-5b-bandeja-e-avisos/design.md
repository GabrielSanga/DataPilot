# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver `specs/avisos/spec.md`.

O que molda a solução:

- O painel monta a tela a cada retrato da pasta de status (a cada 5 segundos ou quando um arquivo muda) e já calcula o status exibido de cada sessão e o resumo do cabeçalho.
- "Aguardando você" é gravado a cada `Stop`, ou seja, a cada resposta do agente. "Precisa de atenção" é gravado nos pedidos de permissão e de escolha.
- Quem trabalha numa aba do Windows Terminal está com o painel fora de foco. Notificar toda resposta do agente nessa situação seria ruído.
- O arquivo de status é escrito pelo usuário `ia`; o arquivo de dados é do usuário principal.
- O PRD sugere um pacote de terceiros para a bandeja. A etapa 6a mostrou o custo de depender de pacotes não oficiais e de passar pela aprovação de TI.
- Decisões do usuário (17/11/2026): fechar a janela encerra o painel; notificações só com o painel fora de foco.

## Goals / Non-Goals

**Goals:**

- Ver de relance, pela bandeja, quantas sessões precisam de atenção.
- Ser avisado de uma sessão que precisa do usuário quando ele não está olhando para o painel, sem uma notificação a cada resposta do agente.
- Nenhuma ação executada a partir do ícone ou da notificação além de mostrar a janela.

**Non-Goals:**

- Manter o painel rodando depois de fechar a janela.
- Som, configuração dos avisos pela tela, histórico de notificações.
- Saber qual aba do Windows Terminal está em foco.

## Decisions

### 1. Bandeja com o que acompanha o .NET

O ícone e as notificações usam o `NotifyIcon` do Windows Forms, habilitado no projeto WPF. No Windows 10, a notificação desse componente aparece como notificação do sistema e respeita o "Assistente de foco". Para não misturar tipos do Windows Forms com os do WPF no código, o projeto remove o `using` global do Windows Forms e o componente é usado por nome completo, num único arquivo.

- Por quê: evita um pacote de terceiros e uma nova aprovação de TI.
- Alternativa descartada: `Hardcodet.NotifyIcon.Wpf`, sugerido no PRD. Mais integrado ao WPF, mas é mais uma dependência externa.
- Alternativa descartada: notificações do Windows App SDK. Exigem registrar o aplicativo e trazem dependências maiores.

### 2. Ícone desenhado pelo painel

O ícone é desenhado em tempo de execução: um círculo cinza quando nenhuma sessão precisa de atenção, e vermelho com o número (1 a 9, ou "9+") quando há. A dica do ícone traz o resumo do cabeçalho ("1 precisa de atenção · 2 executando").

- Por quê: o número precisa mudar com a tela, e o projeto não tem arquivo de ícone.
- A contagem é a mesma do resumo do cabeçalho, para os dois nunca divergirem.

### 3. Fechar encerra, minimizar mantém

Fechar a janela encerra o painel, e o ícone é removido na saída. Minimizar não muda nada além de esconder a janela: o ícone e as notificações continuam. Clicar no ícone, ou em "Abrir painel" no menu dele, restaura e ativa a janela. "Sair" encerra o painel.

- Decisão do usuário. Mantém o comportamento atual de fechar e atende ao critério do PRD de notificar com o painel minimizado.
- O ícone é removido explicitamente na saída, para não sobrar um ícone fantasma até o mouse passar por cima.

### 4. Quando notificar

A cada retrato, uma função pura do Core compara o status exibido de cada sessão com o do retrato anterior e decide:

- **Precisa de atenção:** notifica quando a sessão entra nesse status, vindo de outro status ou surgindo já nele.
- **Aguardando você:** notifica quando a sessão está nesse status há pelo menos 2 minutos sem ter saído dele, uma vez por permanência.
- **Fora de foco:** só notifica quando a janela do painel não está ativa (minimizada ou com outro aplicativo em primeiro plano). Uma decisão tomada com o painel em foco é descartada, não adiada.
- **Abertura:** o primeiro retrato depois de abrir o painel só registra os status; não notifica nada.
- **Repetição:** a mesma sessão não é notificada de novo pelo mesmo status em menos de 2 minutos.

- Por quê a espera de 2 minutos em "aguardando": quem está trabalhando na aba responde em segundos, e a sessão sai de "aguardando" antes de notificar. Quem saiu de perto é avisado de que o agente terminou. É um acréscimo à decisão do usuário de notificar só fora de foco, que sozinha não evitaria uma notificação por resposta enquanto ele trabalha no terminal.
- Por quê sem espera em "atenção": um pedido de permissão para o agente até ser respondido.
- O que entra na comparação é só o status exibido, já calculado para a tela. A regra de inatividade continua valendo: uma sessão que vira "inativa" sai de "aguardando" e não é notificada.

### 5. Uma notificação por ciclo

Quando um retrato produz mais de uma notificação, o painel mostra uma só: com uma sessão, o título é o status e o texto identifica a sessão; com várias, o título diz quantas sessões precisam do usuário e o texto lista até três delas.

- Por quê: notificações empilhadas no mesmo instante escondem umas às outras e cansam.

### 6. Texto só do arquivo de dados

A sessão é identificada pelo que está no arquivo de dados: o `id` da demanda, o repositório e o rótulo, no mesmo formato do título da aba, mais o agente. Uma sessão não vinculada aparece como "Sessão do Codex não vinculada" ou "Sessão do Claude não vinculada".

- Por quê: o arquivo de status é escrito pelo `ia`. Nenhum texto dele, como a pasta de trabalho, aparece na notificação.
- O status vem do arquivo de status, mas só escolhe entre dois títulos fixos.

### 7. Clicar na notificação

Clicar na notificação restaura e ativa a janela e seleciona a demanda da sessão notificada, ou "Não vinculadas". Com uma notificação de várias sessões, seleciona a demanda da primeira listada.

- A notificação nunca abre nem retoma sessão. Qualquer ação continua dependendo de um clique na janela.

### 8. Testes

No Core: a função de decisão com relógio controlado, cobrindo a entrada em atenção, a espera de 2 minutos em aguardando (inclusive a saída antes do prazo), o descarte com o painel em foco, a abertura sem notificação, a repetição dentro de 2 minutos, a sessão que vira inativa, o agrupamento e o texto de sessão vinculada e não vinculada. A contagem do ícone é comparada com a do resumo do cabeçalho. O ícone, a janela e as notificações do Windows são verificados à mão.

## Risks / Trade-offs

- **Notificação de "aguardando" que chega tarde** → é a espera de 2 minutos, de propósito. "Precisa de atenção" não espera.
- **O `ia` alternar o status de propósito para gerar notificações** → limitado a uma notificação por sessão e status a cada 2 minutos e a uma notificação por ciclo; o texto é do arquivo de dados.
- **Notificações escondidas pelo "Assistente de foco"** → é o comportamento do Windows; o ícone da bandeja continua mostrando a contagem.
- **Windows Forms dentro do projeto WPF** → tipos com o mesmo nome nos dois; resolvido removendo o `using` global e isolando o componente num arquivo.
- **Fechar a janela sem querer** → o painel encerra e os avisos param; é o comportamento atual e foi a decisão do usuário.

## Migration Plan

Nada a instalar nem migrar. O ícone aparece na próxima vez que o painel abrir. Pode ser preciso, uma vez, permitir que o ícone fique visível na barra de tarefas, nas configurações do Windows.

## Open Questions

- Manter o painel na bandeja ao fechar a janela, com início junto do Windows.
- Configurar pela tela quais status notificam e a espera de "aguardando".
