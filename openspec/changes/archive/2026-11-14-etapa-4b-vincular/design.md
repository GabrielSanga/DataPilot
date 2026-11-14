# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver as três specs em `specs/`.

O que molda a solução:

- Até aqui, a regra do projeto é absoluta: nada lido de um arquivo de status, escrito pelo usuário `ia`, entra nos dados do painel nem em comandos. O código reflete isso com tipos separados: `Sessao` (pasta de status) e `SessaoRegistrada` (arquivo de dados), e o montador de comandos só aceita o segundo.
- O painel roda como o usuário principal, com acesso aos servidores da empresa. Uma sessão registrada vira, na retomada, argumentos de `abrir.ps1`: agente, pasta e identificador.
- A etapa 4a deixou as sessões que o painel não abriu na entrada "Não vinculadas", sem ações, e a visão agrupada como função pura no Core.
- Resultados conhecidos: o identificador do Codex é um GUID canônico; `/clear` no Claude cria uma sessão com outro identificador; o `abrir.ps1` valida de novo agente, pasta e GUID.

## Goals / Non-Goals

**Goals:**

- Um único ponto no código em que um valor de arquivo de status passa aos dados do painel, pequeno o bastante para ser revisado inteiro.
- Impossibilidade, por construção, de a pasta de trabalho ou outro campo do arquivo de status acompanhar o identificador.
- Cards de repositório que mostram só sessões registradas, sem ruído de sessões antigas da mesma pasta.

**Non-Goals:**

- Vínculo automático, mesmo quando o painel acabou de abrir a sessão. Fica para uma change posterior, baseada num identificador de lançamento gerado pelo painel.
- Sugestões de vínculo no card do repositório.
- Verificar se a sessão realmente existe no agente ou pertence àquele repositório.
- Editar o rótulo ou outros campos de uma sessão já vinculada.

## Decisions

### 1. Um tipo para o pedido de vínculo

O Core ganha `PedidoVinculo`, com dois campos: agente e identificador. Ele só pode ser criado por uma função que recebe uma `Sessao` lida da pasta de status e devolve o pedido apenas se o identificador for um GUID canônico. A operação de `DadosPainel` que registra o vínculo recebe o pedido, o `id` da demanda, a pasta e o rótulo.

- Por quê: mantém a separação de tipos que protege o projeto. A operação de vínculo não recebe uma `Sessao`, então `Cwd`, `Status` e `Atualizado` não têm como ser gravados. O cruzamento inteiro fica numa função de poucas linhas, com testes próprios.
- O agente é um valor de enumeração já validado na leitura do arquivo de status; não há texto livre.
- Alternativa descartada: passar a `Sessao` para `DadosPainel` e extrair os campos lá. Funciona, mas espalha o tipo não confiável pela camada de dados.

### 2. Operação de vínculo reaproveita o registro de sessão

O vínculo grava a sessão pelo mesmo caminho do registro existente, com um campo a mais, `vinculadaEm`. Se o repositório informado ainda não estiver na demanda, é vinculado a ela na mesma gravação.

- Por quê: valida demanda, pasta, GUID e rótulo com o validador único e herda a recusa de identificador já registrado, a gravação atômica e a preservação de campos desconhecidos.
- `vinculadaEm` não é lido pelo painel nesta etapa; serve de rastro para quem inspecionar o arquivo. `criadaEm` recebe a mesma data, porque o painel não conhece a data real de criação da sessão.

### 3. Sem sugestões no card

Os cards de repositório mostram apenas sessões registradas. As sessões não vinculadas aparecem só na entrada "Não vinculadas".

- Por quê: uma primeira versão mostrava no card, como sugestão com "Vincular aqui", toda sessão não vinculada cuja pasta de trabalho tinha o nome do repositório. No uso, uma demanda recém-criada recebia todas as sessões antigas daquela pasta, porque o nome da pasta não diz a que demanda uma sessão pertence. O recurso foi retirado.
- O caso que as sugestões tentavam atender, o Codex recém-aberto pelo painel, passa a ser tratado pelo vínculo automático de uma change posterior.

### 4. Um caminho na interface

"Vincular", em "Não vinculadas", abre um diálogo com demanda, repositório (os da demanda escolhida, com opção de informar um novo) e rótulo. A demanda e o repositório vêm pré-selecionados quando há coincidência de nome com a pasta de trabalho da sessão.

- Por quê: é o único ponto em que o usuário decide o vínculo, com a demanda e o repositório à vista. A pré-seleção só preenche o diálogo; nada é gravado sem a confirmação.
- O aviso de pasta divergente aparece quando o repositório escolhido não coincide com a pasta de trabalho informada pela sessão.
- Sessões com identificador fora do formato aparecem com a ação desabilitada e uma dica com o motivo.

### 5. Avaliação do que o vínculo passa a permitir

Um arquivo de status forjado pelo `ia` pode trazer qualquer agente válido, qualquer identificador e qualquer pasta de trabalho. Com o vínculo:

- **Identificador:** só entra se for GUID canônico. Vira o argumento `-Sessao`, validado de novo no montador e no `abrir.ps1`. Não há como injetar outro argumento ou comando.
- **Agente:** `claude` ou `codex`. Um agente trocado faz a retomada falhar.
- **Pasta de trabalho:** não é gravada. Uma pasta forjada só muda a demanda e o repositório pré-selecionados no diálogo; se o usuário confirmar, a retomada roda no repositório que ele tem cadastrado.
- **Efeito máximo:** o painel executa, como `ia`, `claude --resume` ou `codex resume` com um GUID escolhido pelo `ia`, num repositório escolhido pelo usuário. É algo que o `ia` já pode fazer sozinho; nada é executado como o usuário principal além do `wt.exe` e do `ssh` com argumentos de formato restrito.

O vínculo não amplia o alcance do `ia`. O que ele acrescenta é a possibilidade de confundir o usuário sobre a qual sessão um botão se refere, limitada pela indicação do repositório cadastrado na linha.

### 6. Testes

Testes unitários no Core para: criação do pedido de vínculo (GUID canônico aceito; demais formatos recusados); operação de vínculo (campos gravados, ausência de qualquer outro valor do arquivo de status, `vinculadaEm`, repositório novo, identificador já registrado, demanda arquivada ou inexistente, arquivo ilegível); a visão agrupada (uma sessão não vinculada de mesma pasta não aparece em nenhum card); e a decisão do aviso de pasta divergente.

- Um teste confere o conteúdo do arquivo de dados depois de um vínculo cuja sessão traz uma pasta de trabalho com texto reconhecível, e verifica que esse texto não aparece no arquivo.

## Risks / Trade-offs

- **Um diálogo por sessão do Codex** → é o custo de não haver vínculo automático nesta change. A pré-seleção reduz o diálogo a uma confirmação no caso comum; o vínculo automático de uma change posterior elimina o passo para sessões abertas pelo painel.
- **Usuário vincula a sessão errada por causa de uma pasta de trabalho forjada** → a retomada abre a conversa do identificador vinculado no repositório escolhido; na pior hipótese, o agente não encontra a conversa. "Remover registro" desfaz.
- **Sessão vinculada no repositório errado por engano do usuário** → o aviso de pasta divergente cobre o diálogo; desfazer é remover o registro e vincular de novo.
- **`criadaEm` de sessão vinculada é a data do vínculo** → impreciso, mas o painel não tem fonte confiável melhor; `vinculadaEm` deixa claro o que a data significa.
- **Mudança numa regra que era absoluta** → a exceção fica escrita na spec `dados-do-painel` e concentrada num tipo e numa função, para que a revisão futura tenha um lugar só para olhar.

## Migration Plan

Sem conversão. Sessões já registradas continuam como estão; o campo `vinculadaEm` só aparece em vínculos novos. Reverter é voltar à versão anterior do painel: as sessões vinculadas continuam no arquivo e continuam podendo ser retomadas, porque têm o mesmo formato das demais.

## Open Questions

- Editar o rótulo de uma sessão registrada. Fica para depois do uso.
- Vínculo automático das sessões abertas pelo painel, por um identificador de lançamento que o `abrir.ps1` repassa ao hook. Depende de confirmar que o Codex entrega variáveis de ambiente aos hooks; é assunto de uma change própria.
