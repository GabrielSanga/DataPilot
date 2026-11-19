# Proposal

## Why

No uso diário, o painel pede informações que o usuário não quer manter e mostra mais do que precisa:

- Criar uma demanda exige um identificador que só serve para o painel.
- A change do OpenSpec é informada na demanda, mas cada repositório tem a sua; numa demanda com vários repositórios, a change certa varia por repositório.
- O repositório é digitado pelo nome da pasta, e a change pelo nome, sem ver o que existe.
- Os formulários têm texto de ajuda debaixo de cada campo.
- Trabalhar sem uma demanda não é natural: "Não vinculadas" só permite vincular, não abrir nem retomar.
- Uma sessão que não interessa mais não sai do painel: remover o registro a devolve a "Não vinculadas", porque o arquivo de status continua lá.

## What Changes

- **"Demanda" passa a se chamar "Trabalho" na interface.** O arquivo de dados e o código mantêm os nomes atuais.
- **Criar trabalho pede só o título.** O identificador é gerado pelo painel e não aparece. "Editar" altera só o título. O título da aba do terminal passa a usar o título do trabalho no lugar do identificador.
- **Change do OpenSpec por repositório.** O campo sai do trabalho; cada repositório de um trabalho pode ter a sua change, e a barra de progresso fica no repositório. A soma no topo do trabalho deixa de existir.
- **Vincular repositório abre a escolha de pasta do Windows**, já em `C:\Fontes`, e aceita só uma pasta direta dessa raiz. Escolhida a pasta, o diálogo lista as changes ativas do repositório para o usuário escolher uma, ou nenhuma. A change pode ser trocada depois, pelo menu do repositório.
- **Formulários sem texto de ajuda permanente.** Restrições aparecem só como erro, quando acontecem.
- **Diálogos de nova sessão e de vincular repositório sem a identificação do trabalho e do repositório.**
- **Linha da sessão mais limpa.** Sem "sem rótulo" quando não há rótulo; o status aparece só como uma bolinha colorida à esquerda do nome do agente, com o nome do status na dica e para leitores de tela.
- **"Não vinculadas" vira "Sessões".** Lista as sessões que não estão em nenhum trabalho: as abertas sem trabalho e as que o painel não abriu. Em "Sessões" é possível abrir uma sessão nova (escolhendo a pasta) e retomar qualquer sessão; o usuário trabalha sem "Trabalho" quando quiser.
- **Excluir sessão.** Toda sessão tem um botão "Excluir" (ícone) que tira a sessão do painel de vez: o registro sai do arquivo de dados, o identificador entra numa lista de excluídas, e ela não aparece mais em lugar nenhum nem é vinculada sozinha.
- **Sessões de um trabalho perdem "Mover para outra demanda" e "Remover registro"**, substituídos por "Excluir".
- **BREAKING** O arquivo de dados ganha `openspecChange` em cada repositório e a lista `excluidas`. A change gravada na demanda por versões anteriores continua sendo lida, para os repositórios que não têm a própria.
- **BREAKING** O painel passa a gravar no arquivo de dados identificadores de sessões que o usuário excluiu, inclusive de sessões que ele nunca registrou.

Fora desta change: renomear os termos nas specs e no código, apagar arquivos de status, a bandeja e as notificações (`etapa-5b-bandeja-e-avisos`).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `demandas`: criar e editar só pelo título, vincular repositório pela pasta, change por repositório, excluir sessão; saem mover sessão e remover registro.
- `dados-do-painel`: change no repositório, lista de sessões excluídas, campos e validação da demanda, exceção de gravação de dados de status.
- `lista-de-sessoes`: entrada "Sessões" no lugar de "Não vinculadas", linha da sessão com a bolinha de status, ações da linha, tela do trabalho e filtro sem o identificador.
- `vinculo-de-sessao`: retomar uma sessão que o painel não registrou, vincular a um trabalho a partir de "Sessões", sessão excluída nunca volta; sai desfazer o vínculo.
- `progresso-openspec`: change lida do repositório e progresso só no repositório.
- `lancamento-de-sessao`: título da aba com o título do trabalho, nova sessão também em "Sessões", diálogo de nova sessão só com agente e rótulo.
- `padroes-de-interface`: nomes na interface, formulários sem ajuda permanente, status em texto no alto contraste, confirmação de exclusão.

## Impact

- **Código:** `DadosPainel` (identificador gerado, change no repositório, lista de excluídas), a visão agrupada e a correspondência automática (ignorar excluídas), o montador do título da aba, a leitura do progresso (change do repositório) e uma listagem das changes de um repositório no `DataPilot.Core`; diálogos e janela principal no `DataPilot.Wpf`.
- **Dados:** campos novos opcionais; arquivos anteriores continuam válidos.
- **Scripts:** nenhuma mudança.
- **Segurança:** o painel passa a listar pastas dentro de `C:\Fontes`, área do usuário `ia`, para oferecer as changes. Os nomes listados só viram dado depois de escolhidos pelo usuário e validados, e só são usados para ler o `tasks.md` como hoje. A avaliação está no `design.md`.
- **Dependências:** nenhum pacote novo. A escolha de pasta usa o diálogo do próprio .NET.
