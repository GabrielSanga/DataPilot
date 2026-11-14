# Proposal

## Why

O painel já lista, abre e retoma sessões, mas todas ficam numa lista única. O problema que motivou o projeto continua: uma demanda que toca dois módulos do ERP aparece como sessões soltas, sem nada que diga que pertencem ao mesmo trabalho. Esta change introduz a demanda como unidade principal do painel e reorganiza a tela em torno dela. É a primeira das duas changes da Etapa 4; o vínculo de sessões que o painel não abriu fica para a segunda.

## What Changes

- Demandas no painel: criar, editar, arquivar e desarquivar, com `id` e `titulo` obrigatórios e `openspecChange` opcional.
- Tela reorganizada em duas áreas: à esquerda, a lista de demandas com um resumo de cada uma; à direita, a demanda selecionada, com um card por repositório e as sessões de cada um.
- Repositórios passam a ser vinculados a uma demanda. O mesmo repositório pode estar em várias demandas.
- "Nova sessão" passa a ser acionada no card do repositório; o diálogo pede só o agente e o rótulo.
- Mover uma sessão registrada para outra demanda e remover o registro de uma sessão.
- Sessões registradas aparecem sempre na sua demanda: as antigas não são ocultadas e as que não têm arquivo de status aparecem como "sem status".
- Sessões que o painel não registrou ficam numa entrada "Não vinculadas", com a lista atual e sem ações.
- Título da aba passa a começar pelo `id` da demanda ("CHG-142 · Faturamento · implementação").
- **BREAKING** A tela "Repositórios" da Etapa 3 deixa de existir. Os repositórios já cadastrados continuam na demanda "Sem demanda".
- **BREAKING** A carga do arquivo de dados deixa de ignorar um repositório repetido em demandas diferentes; só a repetição dentro da mesma demanda é desconsiderada.

Fora desta change: vincular sessões não registradas (segunda change da Etapa 4), excluir demandas, progresso do OpenSpec, bandeja e notificações.

## Capabilities

### New Capabilities

- `demandas`: o ciclo de vida das demandas no painel — criar, editar, arquivar, a demanda fixa "Sem demanda", repositórios por demanda, mover e remover sessões.

### Modified Capabilities

- `dados-do-painel`: o arquivo passa a ter várias demandas, com validação dos campos da demanda, repositório repetido entre demandas e sessão única no arquivo.
- `lista-de-sessoes`: a tela passa a ser agrupada por demanda, com resumo e ordem das demandas, a entrada "Não vinculadas", sessões sem arquivo de status e seleção preservada.
- `lancamento-de-sessao`: "Nova sessão" parte do card do repositório, e o título da aba passa a usar o `id` da demanda.

## Impact

- **Código:** o modelo de dados do `DataPilot.Core` ganha a demanda; `DadosPainel` passa a operar por demanda; novas funções puras montam a visão agrupada; a janela principal do `DataPilot.Wpf` é reorganizada e ganha diálogos de demanda, de vincular repositório e de mover sessão. A janela "Repositórios" é removida.
- **Dados existentes:** o `demandas.json` atual, só com "Sem demanda", é lido sem conversão. Os campos novos são opcionais na carga.
- **Scripts e pasta de status:** sem alteração.
- **Segurança:** nenhuma fonte nova de dados entra em comandos. O `id` da demanda vai para o título da aba e é digitado pelo usuário principal, com conjunto de caracteres restrito.
- **Dependências:** nenhum pacote novo.
