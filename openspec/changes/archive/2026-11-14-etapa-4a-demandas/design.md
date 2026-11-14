# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver as quatro specs em `specs/`.

O que molda a solução:

- A Etapa 3 deixou o arquivo de dados já no formato do PRD (`demandas` → `repositorios` → `sessoes`), com uma única demanda, `sem-demanda`. `DadosPainel` lê o arquivo como árvore JSON, preserva o que não reconhece e grava de forma atômica.
- Três pontos do código atual precisam mudar para haver várias demandas:
  - a carga considera cada pasta uma única vez em todo o arquivo;
  - as operações de `DadosPainel` localizam o repositório só pela pasta;
  - `SessaoRegistrada` e `RepositorioCadastrado` não sabem a que demanda pertencem.
- A janela atual é uma lista única de sessões, atualizada no lugar a cada retrato da pasta de status. Na Etapa 3 houve um defeito de seleção perdida porque uma coleção era recriada a cada releitura.
- As regras de exibição (status exibido, ordem, filtro de antigas, resumo) já são funções puras no Core.
- A regra de segurança continua a mesma: comandos só com dados do arquivo de dados. Esta change não cria nenhum caminho novo de dados do `ia` para comandos; isso é assunto da segunda change da Etapa 4.

## Goals / Non-Goals

**Goals:**

- A demanda como tipo de primeira classe no Core, com toda operação identificando demanda e repositório.
- A visão agrupada inteira (demandas, cards, sessões, resumos, ordem, não vinculadas) produzida por uma função pura e testada.
- Ler o arquivo de dados existente sem conversão.
- Atualizações de tela que nunca derrubam a seleção.

**Non-Goals:**

- Vincular sessões não registradas.
- Excluir demandas.
- Arrastar e soltar, busca ou filtros além de "arquivadas" e "antigas".
- Guardar preferências de tela (demanda selecionada, tamanho da janela).

## Decisions

### 1. Demanda no modelo e operações endereçadas por demanda

O Core ganha `Demanda` (`Id`, `Titulo`, `OpenspecChange`, `CriadaEm`, `Arquivada`, `Repositorios`). `SessaoRegistrada` ganha `DemandaId`. O estado carregado passa a ser uma lista de demandas. Em `DadosPainel`, toda operação sobre repositório recebe o `id` da demanda e a pasta; operações sobre sessão recebem o identificador da sessão, que é único no arquivo.

- Por quê: com a mesma pasta em várias demandas, a pasta sozinha deixa de identificar um repositório. O identificador da sessão continua bastando, e a unicidade dele no arquivo garante que mover e remover nunca atinjam o registro errado.
- A carga troca a deduplicação global de pastas por deduplicação dentro de cada demanda, e passa a deduplicar identificadores de sessão no arquivo inteiro (a primeira ocorrência vale).
- Operações novas: criar, editar, arquivar e desarquivar demanda; vincular e remover repositório por demanda; mover sessão; remover sessão. Todas seguem o padrão existente de reler, alterar a árvore e gravar por temporário e troca.

### 2. Regras da demanda no validador único

`ValidadorEntrada` ganha as regras de `id` (a da pasta, com limite de 30), `titulo` (1 a 80, sem caracteres de controle) e `openspecChange` (regra do `id`).

- Por quê: o `id` vai para o título da aba, que é um argumento do `wt.exe`. A regra de caracteres da pasta já exclui `;`, espaço e aspas, então o título continua sem precisar de escape. O `openspecChange` será usado na Etapa 5 para montar um caminho; validá-lo desde já evita aceitar hoje um valor que amanhã seria perigoso.
- O `titulo` é só exibido; não entra em comando nem em caminho.

### 3. `sem-demanda` como demanda comum com restrições

A demanda `sem-demanda` é carregada e exibida como qualquer outra. As restrições (não editar, não arquivar, título da aba com o nome do agente) ficam em funções do Core que consultam o `id`.

- Por quê: evita um segundo caminho de código para as sessões da Etapa 3. A interface só precisa perguntar ao Core quais ações a demanda admite.
- O painel não cria `sem-demanda` nesta change; ela só existe se veio do arquivo. A segunda change pode voltar a criá-la.

### 4. Título da aba a partir da demanda

`MontadorComando` passa a receber o `id` da demanda junto com o repositório. O título é `id · pasta [· rótulo]`, com o nome do agente no lugar do `id` quando a demanda é `sem-demanda`.

- Por quê: é o formato do PRD e o que permite achar a aba pela demanda. O `id` vem do arquivo de dados e é validado de novo no montador, como os demais valores.
- A ordem e o conteúdo dos demais argumentos não mudam.

### 5. Visão agrupada como função pura

Uma função do Core recebe as demandas, o retrato da pasta de status, as opções (mostrar arquivadas, mostrar antigas) e o relógio, e devolve a visão completa: demandas ordenadas com seus resumos, cada uma com seus cards e sessões, e a lista de não vinculadas.

- Por quê: é onde estão as regras com mais casos (status mais urgente por demanda, ordem, sessão sem arquivo de status, o que conta como não vinculada). Como função pura, é testada sem tela, do mesmo jeito que as regras da Etapa 2.
- A correspondência entre sessão da lista e sessão registrada continua sendo por identificador e agente. Uma sessão da pasta de status que não corresponde a nenhuma registrada vai para "Não vinculadas".
- "Sem status" entra como mais um valor do status exibido, o último na ordem, em cinza. Ele só existe para sessões registradas sem arquivo de status e não conta no resumo do cabeçalho.
- O filtro de antigas passa a valer só para as não vinculadas; sessões registradas aparecem sempre.

### 6. Janela em duas áreas, atualizada no lugar

A janela principal passa a ter a lista de demandas à esquerda e a demanda selecionada à direita. A entrada "Não vinculadas" fica no fim da lista da esquerda e, selecionada, mostra à direita a lista simples da Etapa 2, sem botões. Os view models (demanda, card, sessão) são atualizados no lugar, por chave: `id` da demanda, pasta dentro da demanda e identificador da sessão.

- Por quê: atualizar por chave, em vez de recriar coleções, é o que mantém a seleção e a rolagem; é a lição do defeito da Etapa 3, agora tratada como requisito.
- Ações ficam onde o usuário as espera: "Nova demanda" e "Mostrar arquivadas" na lista; "Editar", "Arquivar" e "Vincular repositório" no topo da demanda; "Nova sessão" e "Remover" no card; "Retomar", "Mover" e "Remover" na linha da sessão.
- Diálogos pequenos: demanda (criar e editar), vincular repositório (campo com sugestões das pastas já usadas), nova sessão (agente e rótulo) e mover sessão (escolha da demanda de destino). A janela "Repositórios" é removida.
- Demandas arquivadas, quando visíveis, não oferecem "Nova sessão" nem "Vincular repositório"; "Retomar" continua disponível.

### 7. Testes

Testes unitários no Core para: validação dos campos da demanda; carga com várias demandas, pasta repetida entre demandas e na mesma demanda, sessão repetida, demanda inválida e arquivo da Etapa 3; cada operação nova de `DadosPainel`, incluindo mover para demanda sem o repositório e a preservação de campos desconhecidos; título da aba; e a visão agrupada (resumo, ordem das demandas e das sessões, sem status, não vinculadas, arquivadas, antigas).

- A interface é verificada por roteiro manual, com `--dados` e `--pasta-status` apontando para dados de exemplo e depois com dados reais.

## Risks / Trade-offs

- **Reorganização grande da janela sem testes automatizados de interface** → toda a lógica de agrupamento fica no Core, testada; a tela só liga dados. O roteiro manual cobre seleção preservada, que é o ponto que já falhou uma vez.
- **Arquivo de dados real em uso** → a carga aceita o formato atual sem conversão, e o primeiro teste de carga usa um arquivo no formato gravado pela Etapa 3. Recomenda-se copiar o `demandas.json` antes do primeiro uso da versão nova.
- **`id` editável muda o título de abas futuras, mas não o das já abertas** → efeito aceito; o título é só um rótulo.
- **Sessões do Codex e pós-`/clear` continuam sem ações** → ficam visíveis em "Não vinculadas", com a contagem na lista, até a segunda change.
- **Mover sessão cria repositório na demanda de destino** → comportamento explícito na spec; evita um diálogo a mais. A pasta é a da própria sessão, vinda do arquivo de dados.
- **Duas instâncias do painel** → continua valendo o que a Etapa 3 aceitou: relê antes de gravar, a última gravação vence.

## Migration Plan

Sem conversão de dados. Ao atualizar o painel, o `demandas.json` existente é lido como está, com a demanda "Sem demanda" e suas sessões. Para reverter, basta voltar à versão anterior do painel: ela ignora demandas adicionais só no sentido de que, ao encontrar a mesma pasta em mais de uma demanda, considera a primeira; os dados não são perdidos, porque campos e entradas desconhecidos são preservados na gravação.

## Open Questions

- Guardar a última demanda selecionada entre execuções. Fica para depois do uso diário.
- Onde mostrar o progresso do OpenSpec na lista de demandas. Assunto da Etapa 5.
