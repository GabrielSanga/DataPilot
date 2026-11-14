# Proposal

## Why

O painel só consegue retomar e mostrar nas demandas as sessões que ele mesmo abriu com um identificador conhecido. Ficam de fora todas as sessões do Codex, cujo identificador só aparece pelo hook, a sessão nova que o Claude cria depois de um `/clear` e qualquer sessão aberta à mão. Elas aparecem em "Não vinculadas", sem ações: uma sessão do Codex aberta pelo próprio painel não mostra status na demanda nem pode ser retomada. Esta change fecha a Etapa 4 dando a essas sessões um vínculo explícito com uma demanda.

É a única mudança do projeto em que um dado escrito pelo usuário `ia` passa a constar nos dados do painel e, por eles, a montar comandos. Por isso é pequena e tratada à parte.

## What Changes

- Ação "Vincular" nas sessões de "Não vinculadas": o usuário escolhe a demanda, o repositório e, opcionalmente, um rótulo, e a sessão passa a ser registrada.
- Do arquivo de status cruzam para o arquivo de dados apenas o identificador da sessão, se for um GUID canônico, e o agente. Demanda, repositório e rótulo vêm sempre do usuário.
- O vínculo nunca é automático, nem para sessões do Codex abertas pelo próprio painel.
- Aviso com confirmação quando a pasta de trabalho informada pela sessão não coincide com o repositório escolhido.
- A sessão vinculada é gravada com o campo `vinculadaEm`, que a distingue das sessões abertas pelo painel.
- **BREAKING** O requisito "Dados de status nunca são gravados" deixa de ser absoluto: passa a admitir essa única exceção, sempre por ação explícita do usuário.

Fora desta change: sugestões de vínculo no card do repositório, que chegaram a ser implementadas e foram retiradas por poluírem as demandas com sessões antigas da mesma pasta; vínculo automático das sessões abertas pelo painel, previsto para uma change posterior; progresso do OpenSpec, bandeja e notificações.

## Capabilities

### New Capabilities

- `vinculo-de-sessao`: associar a uma demanda uma sessão que o painel não abriu — o que pode ser vinculado, o que cruza do arquivo de status para os dados do painel, confirmações e rastro.

### Modified Capabilities

- `dados-do-painel`: o requisito "Dados de status nunca são gravados" passa a permitir a gravação do identificador e do agente no vínculo explícito.
- `lista-de-sessoes`: a entrada "Não vinculadas" passa a oferecer a ação de vincular.

## Impact

- **Código:** um tipo novo no `DataPilot.Core` que representa o pedido de vínculo e carrega só identificador e agente; uma operação nova em `DadosPainel`; botão e diálogo no `DataPilot.Wpf`.
- **Dados:** sessões vinculadas ganham o campo `vinculadaEm`. Arquivos existentes continuam válidos.
- **Segurança:** altera o alcance de RS-1 e RS-2 do PRD de forma controlada. O identificador vinculado passa a ser argumento de `abrir.ps1`, que o valida de novo como GUID. O risco é avaliado no `design.md`.
- **Scripts e pasta de status:** sem alteração.
- **Dependências:** nenhum pacote novo.
