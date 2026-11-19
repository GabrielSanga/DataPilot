# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver `specs/progresso-openspec/spec.md`.

O que molda a solução:

- O `openspecChange` de uma demanda é validado desde a Etapa 4a com a regra de nome de pasta (`[A-Za-z0-9_.-]`, não só pontos, até 100 caracteres). A pasta de cada repositório segue a mesma regra. Nenhum dos dois pode conter separador nem ser `..`.
- Os repositórios ficam em `C:\Fontes`, o clone próprio do usuário `ia`. A raiz é fixa no `abrir.ps1` e nunca vem por parâmetro. Verificado em 17/11/2026: o usuário principal consegue listar `C:\Fontes`; a pasta dá permissão de modificação a "Usuários autenticados", então o `ia` pode criar, trocar e apagar qualquer coisa dentro dela, inclusive junções.
- Verificado na mesma data: o modo de desenvolvedor do Windows está desligado. Sem ele, criar link simbólico exige um privilégio que usuários comuns não têm; junções, que só apontam para pastas locais, qualquer usuário pode criar.
- O OpenSpec 2.0 move a change arquivada para `openspec\changes\archive\AAAA-MM-DD-<nome>`. O próprio CLI conta como concluídas só as caixas marcadas com `x` ou `X`.
- A regra central do projeto: nada escrito pelo `ia` vira ação executada como o usuário principal. Os arquivos de status já são lidos com limite de tamanho, sem bloquear quem escreve e ignorando atalhos.

## Goals / Non-Goals

**Goals:**

- Mostrar, na demanda selecionada, quantas tarefas da change estão concluídas, no total e por repositório.
- Ler o `tasks.md` sem que uma pasta preparada pelo `ia` faça o painel acessar outro lugar.
- Atualizar o progresso no mesmo ritmo da tela, sem vigiar pastas do `ia`.

**Non-Goals:**

- Mostrar o texto das tarefas ou abrir o `tasks.md`.
- Progresso na lista de demandas.
- Ler o progresso de demandas que não estão selecionadas.

## Decisions

### 1. Caminho montado só com dados do painel

O caminho é `<raiz>\<pasta>\openspec\changes\<openspecChange>\tasks.md`, com `<raiz>` igual a `C:\Fontes`, uma constante do painel. `--raiz-fontes` permite apontar para uma pasta de exemplo, como `--pasta-status` já faz. O leitor revalida `pasta` e `openspecChange` antes de montar o caminho.

- Por quê: é a regra que já vale para o comando do terminal. Nenhum valor do arquivo de status entra no caminho.
- O parâmetro `--raiz-fontes` vem de quem inicia o painel, que é o próprio usuário principal.

### 2. Change arquivada como alternativa

Se a pasta da change ativa não existir, o leitor lista `openspec\changes\archive`, escolhe as pastas cujo nome é exatamente `AAAA-MM-DD-<openspecChange>` com uma data válida e usa a de data mais recente.

- Por quê: depois de arquivar, a demanda ainda mostra que a change terminou, em vez de "não encontrada".
- A change ativa tem prioridade: se as duas existirem, vale a ativa.
- A listagem compara nomes como texto e não segue nenhuma entrada que seja atalho ou junção.

### 3. Leitura sem seguir atalhos nem junções

Antes de abrir, o leitor confere cada trecho do caminho a partir de `<raiz>\<pasta>` (a pasta do repositório, `openspec`, `changes`, a pasta da change, `archive` quando for o caso, e o próprio `tasks.md`) e recusa qualquer um que seja atalho ou junção. Depois de abrir, confere que o arquivo aberto é exatamente o do caminho montado, comparando o caminho final do arquivo aberto, obtido do sistema, com o esperado.

- Por quê: o `ia` controla essas pastas. Uma junção poderia levar o painel a ler, como usuário principal, um arquivo fora de `C:\Fontes`.
- A conferência depois de abrir cobre a troca de uma pasta por junção entre a verificação e a abertura.
- O efeito máximo de uma junção que escapasse seria o painel contar caixas de seleção num arquivo local; o conteúdo nunca é exibido. Um link simbólico para um caminho de rede, que faria o painel se autenticar num servidor, depende de um privilégio que o `ia` não tem; a verificação manual confere isso.
- A raiz `C:\Fontes` em si não é verificada: ela é criada pela instalação, como `C:\Painel`.

### 4. Limites da leitura

O arquivo é aberto só para leitura, compartilhando escrita e exclusão, até 256 KB. Maior que isso, ou com UTF-8 inválido, é tratado como ilegível. Nada é gravado.

- Por quê 256 KB: um `tasks.md` grande deste projeto tem cerca de 10 KB; o limite folgado evita recusar arquivos legítimos e impede ler um arquivo enorme a cada 5 segundos.

### 5. Contagem

São contadas as linhas que casam com `^\s*-\s\[( |x|X)\]`. Marcadas são as com `x` ou `X`. Outras linhas são ignoradas.

- Por quê: é a regra do PRD e coincide com a do CLI do OpenSpec. O resultado são só dois números.

### 6. Exibição: soma e por card

A área da demanda selecionada mostra, abaixo do nome da change, uma barra com a soma das tarefas dos repositórios em que a change foi encontrada e lida ("7 de 12 tarefas"). Cada card desses repositórios mostra o progresso dele ("4 de 6"). Uma change arquivada recebe a indicação "arquivada". Quando nenhum repositório tem a change, a área mostra "change não encontrada nos repositórios". Quando um repositório tem a change mas o arquivo não pôde ser lido, o card mostra "progresso indisponível" e esse repositório fica fora da soma.

- Decisão do usuário: soma no topo e detalhe por card.
- Uma change sem caixas de seleção aparece como "sem tarefas".
- Demanda sem `openspecChange` não mostra nada de progresso.

### 7. Atualização por releitura, só da demanda selecionada

A cada atualização da tela (o retrato da pasta de status, a cada 5 segundos ou por mudança detectada) e ao trocar de demanda, o painel relê o progresso da demanda selecionada. O leitor guarda em cache o resultado por caminho, com a data de modificação e o tamanho, e só lê o conteúdo de novo quando um deles muda.

- Por quê: vigiar pastas do `ia` com `FileSystemWatcher` aumentaria o que o painel observa ali. A releitura periódica já é o mecanismo de garantia da tela.
- Só a demanda selecionada: é a única em que o progresso aparece, e isso limita a leitura a poucos arquivos.
- A leitura é rápida e local; uma falha vira "progresso indisponível", sem travar a tela.

### 8. Testes

No Core: a contagem (marcadas, não marcadas, maiúsculas, indentação, linhas que parecem tarefa mas não casam, arquivo sem tarefas); a localização (ativa, arquivada, várias arquivadas, ativa e arquivada ao mesmo tempo, nada encontrado, nomes parecidos que não casam); a leitura segura (junção na pasta do repositório, na pasta da change e no lugar do `tasks.md`, criadas com `mklink /J` numa pasta temporária; arquivo acima do limite; UTF-8 inválido; arquivo aberto para escrita por outro processo); a montagem da soma e do progresso por card; e o cache.

## Risks / Trade-offs

- **Junção preparada pelo `ia`** → recusada antes de abrir e conferida depois de abrir.
- **O `ia` ganhar o privilégio de criar links simbólicos** (modo de desenvolvedor ligado, por exemplo) → a conferência depois de abrir continua valendo, mas a abertura de um caminho de rede já teria acontecido. A verificação manual confere o privilégio; o guia de instalação passa a registrar que o modo de desenvolvedor deve ficar desligado.
- **Progresso falso escrito pelo `ia`** → o `ia` pode marcar tarefas no `tasks.md`; o painel mostra o que está no arquivo. É só exibição, como o status.
- **Mesmo nome de change em dois repositórios com trabalhos diferentes** → a soma mistura os dois; o detalhe por card mostra cada um.
- **Change arquivada com outro formato de nome numa versão futura do OpenSpec** → aparece como "não encontrada". Repetir a verificação depois de atualizar o OpenSpec.

## Migration Plan

Nada a instalar nem migrar. O painel novo passa a mostrar o progresso das demandas que já têm `openspecChange`.

## Open Questions

- Mostrar o progresso também na lista de demandas.
- Abrir o `tasks.md` num visualizador somente leitura a partir do painel.
