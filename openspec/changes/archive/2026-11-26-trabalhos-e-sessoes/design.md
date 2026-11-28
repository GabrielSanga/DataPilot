# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver as specs em `specs/`.

O que molda a solução:

- O arquivo de dados (`demandas.json`), o código (`Demanda`, `DadosPainel`) e as specs usam "demanda". A demanda implícita `sem-demanda` já guarda repositórios e sessões abertas fora de uma demanda.
- O identificador da demanda aparece hoje no título da aba (`CHG-142 · Faturamento`) e é a chave da demanda no arquivo.
- A change do OpenSpec é um campo da demanda, validado com a regra de nome de pasta e usado só para montar o caminho do `tasks.md` (etapa 5a).
- O `abrir.ps1` só aceita como repositório uma pasta direta de `C:\Fontes`, com nome validado. Essa raiz é do usuário `ia`, que pode criar, trocar e apagar qualquer coisa dentro dela.
- O painel nunca grava na pasta de status. Um arquivo de status continua existindo depois que o painel esquece a sessão, por isso hoje "Remover registro" devolve a sessão a "Não vinculadas".
- O .NET 8 em diante traz um diálogo de escolha de pasta para WPF (`Microsoft.Win32.OpenFolderDialog`), sem Windows Forms nem pacote extra.
- A etapa de design aprovou a direção "cockpit escuro", com o status em texto e uma faixa colorida para "precisa de atenção" e "aguardando você".

## Goals / Non-Goals

**Goals:**

- Criar um trabalho e vincular repositórios sem digitar nada além do título.
- Cada repositório com a própria change e o próprio progresso.
- Trabalhar sem trabalho: abrir, retomar e excluir sessões em "Sessões".
- Tirar do painel, de vez, uma sessão que não interessa mais.

**Non-Goals:**

- Renomear "demanda" no arquivo de dados, no código ou nas specs.
- Apagar arquivos de status ou conversas dos agentes.
- Mudar scripts ou hooks.

## Decisions

### 1. "Trabalho" só na interface

A interface mostra "Trabalho", "Novo trabalho", "Editar trabalho", "Vincular a um trabalho". O arquivo de dados, as classes e as specs continuam com "demanda"; uma regra em `padroes-de-interface` diz como cada termo aparece na tela.

- Por quê: renomear o arquivo e o código obrigaria a migrar dados de quem já usa, sem ganho para o usuário. As specs ficam com o termo atual para não reescrever dezenas de cenários que não mudam de comportamento.
- O mesmo vale para "Não vinculadas", que aparece como "Sessões".

### 2. Identificador gerado e escondido

Ao criar um trabalho, o painel gera o identificador `t` seguido de 8 dígitos hexadecimais de um GUID novo, conferindo que não colide com outro identificador. Ele continua sendo a chave no arquivo e nunca aparece na tela nem é editável. Demandas existentes mantêm os identificadores que já têm.

- Por quê manter um identificador: as sessões, os lançamentos e o vínculo usam a demanda como chave, e o título pode repetir ou mudar.
- O formato cabe na regra atual do `id` (até 30 caracteres de `[A-Za-z0-9_.-]`), então a validação da carga não muda.

### 3. Título da aba com o título do trabalho

O título da aba passa a ser `<título do trabalho> · <pasta>`, seguido do rótulo quando houver. O título do trabalho entra com no máximo 40 caracteres e com cada `;` trocado por `,`. Em "Sessões" (demanda `sem-demanda`), o nome do agente continua no lugar do título.

- Por quê trocar o `;`: o título do trabalho aceita `;`, e o `wt.exe` trata `;` como separador de comandos. O título vai como argumento separado, mas o próprio `wt.exe` interpreta o texto. A troca acontece no montador, que já revalida tudo antes de lançar.
- O identificador gerado não diz nada para quem lê a aba; o título diz.

### 4. Change no repositório, com leitura das antigas

Cada repositório de uma demanda pode ter `openspecChange`, validado com a mesma regra de hoje. O campo da demanda deixa de ser gravado. Na carga, um repositório sem change própria herda a change da demanda, se o arquivo ainda tiver uma, gravada por versão anterior. Ao trocar a change de um repositório, só o repositório é gravado.

- Por quê herdar em vez de migrar: não reescreve o arquivo na abertura, e quem volta para a versão anterior não perde a informação.
- O progresso (etapa 5a) passa a usar a change de cada repositório. A soma no topo deixa de existir: cada repositório tem a própria change, e somar changes diferentes não diz nada.

### 5. Vincular repositório pela pasta

"Vincular repositório" abre o diálogo de escolha de pasta do Windows, começando na raiz dos fontes. O painel aceita a pasta escolhida só se ela for filha direta da raiz, comparando os caminhos completos sem diferenciar maiúsculas, e se o nome seguir a regra de pasta. Senão, mostra o erro e oferece escolher de novo. Só o nome da pasta é gravado, como hoje.

- Por quê a restrição: o `abrir.ps1` só abre agentes em pastas diretas de `C:\Fontes`. Aceitar outra pasta criaria um repositório que nunca abriria.
- A escolha é do usuário principal; nada vem do arquivo de status.

### 6. Lista de changes do repositório

Depois de escolhida a pasta, o diálogo lista as pastas de `<raiz>\<pasta>\openspec\changes`, sem `archive`, como opções, mais "Nenhuma". A lista inclui só entradas que são pastas comuns (nem atalho nem junção, conferindo também `openspec` e `changes`), cujo nome segue a regra de `openspecChange`, até 200, em ordem alfabética. Sem a pasta `openspec\changes`, a lista mostra só "Nenhuma". O mesmo diálogo, sem a escolha de pasta, serve para "Trocar change" no menu do repositório.

- Esses nomes vêm de uma pasta controlada pelo usuário `ia`. Eles só são exibidos como opções; viram dado só quando o usuário escolhe um, e então passam pela validação da change antes de gravar. Depois, são usados apenas para montar o caminho do `tasks.md`, que já é lido sem seguir atalhos nem junções e só para contar.
- O pior que o `ia` consegue é fazer aparecer na lista um nome de change que o usuário não criou. O usuário vê o nome antes de escolher.

### 7. "Sessões" no lugar de "Não vinculadas"

A entrada fixa no fim da lista passa a se chamar "Sessões" e reúne:

- as sessões registradas na demanda `sem-demanda`, com o repositório de cada uma;
- os lançamentos pendentes da `sem-demanda`;
- as sessões lidas da pasta de status que não estão registradas em nenhuma demanda nem excluídas.

Ações:

- **Nova sessão**, no topo da área: abre a escolha de pasta (decisão 5) e o diálogo de nova sessão. A pasta é vinculada à `sem-demanda`, se ainda não estiver, e a sessão segue o fluxo de sempre (Claude registrado na hora, Codex com lançamento pendente).
- **Retomar** numa sessão registrada: igual ao de um trabalho.
- **Retomar** numa sessão não registrada: pede o repositório, com as pastas diretas da raiz pré-selecionando a que tem o mesmo nome do último segmento da pasta de trabalho, como o diálogo de vínculo já faz. Registra a sessão na `sem-demanda`, nesse repositório, com `vinculadaEm`, e retoma. Só para identificador GUID canônico.
- **Excluir** em todas.
- **Vincular a um trabalho**, no menu de mais ações: o diálogo de vínculo atual. Uma sessão registrada na `sem-demanda` é movida para o trabalho escolhido.

- Por quê pedir o repositório para retomar: o comando nunca usa valores do arquivo de status. A pasta de trabalho só pré-seleciona a opção, como já acontece no vínculo.
- A `sem-demanda` não aparece mais na lista de trabalhos: ela é "Sessões".

### 8. Excluir sessão

"Excluir" (ícone de lixeira, com nome acessível "Excluir sessão") pede confirmação e então, numa única gravação, remove o registro da sessão, se houver, e acrescenta à lista `excluidas` da raiz do arquivo de dados um item com `agente`, `sessionId` e `excluidaEm`. A visão, o resumo, o alerta e a correspondência automática ignoram as sessões excluídas.

- O identificador entra na lista se casar com a regra do hook (`[A-Za-z0-9_-]`, até 100 caracteres), para que qualquer sessão exibida possa ser excluída. Ele só é comparado; nunca entra em comando nem em caminho.
- A confirmação diz que a conversa não é apagada e que a sessão some do painel. "Cancelar" continua sendo o padrão.
- Um lançamento pendente continua com "Abrir" e "Cancelar"; "Excluir" vale para sessões.
- O arquivo de status continua na pasta de status; o painel não grava lá.
- A lista não é podada nesta change; cada item tem cerca de 100 bytes.

### 9. Linha da sessão

A linha mostra: a bolinha do status, o agente, o rótulo (em branco quando não há), o tempo decorrido, a ação principal e o botão "Excluir". As sessões em "precisa de atenção" e "aguardando você" mantêm a faixa colorida à esquerda.

- O nome do status deixa de aparecer em texto na linha, a pedido do usuário. Para a cor não ser a única informação: o nome do status fica na dica da linha e no nome acessível da bolinha; os dois status que pedem o usuário têm a faixa, que os distingue dos demais mesmo para quem não diferencia vermelho de verde; com o alto contraste do Windows ligado, o nome do status volta a aparecer em texto.
- O resumo do topo e o alerta continuam em texto.

### 10. Formulários sem ajuda permanente

Os diálogos deixam de mostrar texto debaixo dos campos. As restrições continuam valendo e aparecem como erro junto do campo quando violadas. Os campos opcionais continuam marcados como "(opcional)". O diálogo de nova sessão mostra só o agente e o rótulo; o de vincular repositório, só a pasta e a change.

### 11. Ações em cada lugar

| Lugar | Visível | Menu "Mais ações" |
|---|---|---|
| Sessão de um trabalho | Retomar, Excluir | (sem menu) |
| Sessão em "Sessões" | Retomar, Excluir | Vincular a um trabalho |
| Lançamento pendente | Abrir | Cancelar |
| Repositório | Nova sessão | Trocar change, Remover repositório |
| Trabalho | Vincular repositório | Editar, Arquivar ou Desarquivar |
| Área "Sessões" | Nova sessão | (sem menu) |

### 12. Testes

No Core: geração do identificador (formato e colisão); criação e edição só com título; change no repositório (gravação, validação, herança da demanda); listagem das changes (atalho, junção, nome inválido, `archive`, limite, pasta ausente); exclusão (com e sem registro, identificador fora da regra, repetida); visão, resumo e correspondência ignorando excluídas; "Sessões" com registradas da `sem-demanda`, lançamentos e não registradas; retomar não registrada (registro na `sem-demanda`); título da aba com título, troca de `;` e limite. A tela é verificada por capturas com os dados de exemplo, como na change de design.

## Risks / Trade-offs

- **Specs com "demanda" e "Não vinculadas"** → a regra de nomes em `padroes-de-interface` faz a ponte; renomear nas specs fica para depois.
- **Status sem texto na linha** → dica, nome acessível, faixa e texto no alto contraste. Ainda assim, quem não diferencia cores precisa da dica para distinguir "executando" de "ativa".
- **Título do trabalho repetido** → duas abas com o mesmo título; o rótulo diferencia.
- **Lista de excluídas sempre crescendo** → alguns kilobytes por ano de uso; poda fica como questão em aberto.
- **Nome de change plantado pelo `ia`** → só aparece como opção e passa pela validação; o efeito máximo é o progresso de uma change que o usuário escolheu.
- **Sessão excluída por engano** → a conversa continua no agente; tirar o identificador da lista em `demandas.json` a traz de volta. Não há "desfazer" na tela nesta change.

## Migration Plan

Nada a instalar. Arquivos de dados existentes continuam válidos: demandas antigas mantêm o identificador, e a change da demanda continua valendo para os repositórios que não têm a própria. Voltar à versão anterior: os campos novos são ignorados e preservados; sessões excluídas voltariam a aparecer.

## Open Questions

- Desfazer a exclusão pela tela.
- Podar a lista de excluídas.
- Renomear "demanda" nas specs e no código.
