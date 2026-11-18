# Proposal

## Why

Uma demanda já guarda o nome da change do OpenSpec (`openspecChange`), mas o painel não mostra em que ponto ela está. Para saber quantas tarefas faltam, é preciso abrir o `tasks.md` no repositório. O PRD prevê o progresso da change como um dos objetivos do MVP ("7 de 12 tarefas").

## What Changes

- O painel lê o `tasks.md` da change de cada repositório da demanda selecionada, em `C:\Fontes\<pasta>\openspec\changes\<openspecChange>\tasks.md`, e conta as tarefas marcadas e o total.
- Quando a change já foi arquivada, o painel usa o `tasks.md` da pasta `openspec\changes\archive\AAAA-MM-DD-<openspecChange>` e indica que ela está arquivada.
- A área da demanda selecionada mostra uma barra com a soma dos repositórios em que a change foi encontrada ("7 de 12 tarefas"), e cada card desses repositórios mostra o progresso dele.
- Quando a change não é encontrada em nenhum repositório, o painel indica isso; quando o arquivo não pode ser lido com segurança, indica no card.
- O progresso se atualiza em até 5 segundos depois que o `tasks.md` muda.
- A raiz dos fontes passa a ser uma constante do painel, `C:\Fontes`, a mesma do `abrir.ps1`, com o parâmetro `--raiz-fontes` para testes.

O `tasks.md` fica na pasta do usuário `ia` e é escrito por ele. O painel o trata como os arquivos de status: lê só para exibir números, com limite de tamanho, sem seguir atalhos nem junções no caminho e sem nunca gravar nada ali.

Fora desta change: a bandeja e as notificações, planejadas na change `etapa-5b-bandeja-e-avisos`; progresso na lista de demandas; mostrar o texto das tarefas.

## Capabilities

### New Capabilities

- `progresso-openspec`: localização, leitura segura e contagem do `tasks.md` da change de uma demanda, e a exibição do progresso na demanda selecionada.

### Modified Capabilities

Nenhuma.

## Impact

- **Código:** um leitor do `tasks.md` e a contagem no `DataPilot.Core`; a demanda selecionada e os cards no `DataPilot.Wpf` ganham o progresso; o gerador de exemplos passa a criar uma raiz de fontes de exemplo.
- **Dados:** nenhum campo novo. O `openspecChange` já é validado desde a Etapa 4a com a mesma regra de nome de pasta, justamente para compor este caminho.
- **Segurança:** o painel passa a ler um arquivo de uma pasta controlada pelo `ia`. O caminho é montado só com valores do arquivo de dados; o conteúdo vira apenas duas contagens; atalhos e junções são recusados. A avaliação está no `design.md`.
- **Scripts e dependências:** nenhuma mudança e nenhum pacote novo.
