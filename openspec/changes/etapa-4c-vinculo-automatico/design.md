# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver as quatro specs em `specs/`.

O que molda a solução:

- O identificador de uma sessão do Codex é criado pelo próprio Codex e só chega ao painel pelo arquivo de status, escrito pelo usuário `ia`. O painel não tem como conhecê-lo antes de abrir a sessão.
- O Codex só roda o `SessionStart` ao enviar a primeira mensagem. O arquivo de status de uma sessão nova do Codex surge nesse momento, não ao abrir a aba.
- A primeira versão desta change marcava a sessão com um identificador de lançamento, levado ao hook por variável de ambiente. O ensaio com o codex-cli 0.169.0 mostrou que a variável não chega ao hook. No código dessa versão, o hook é iniciado com o ambiente zerado e recebe o ambiente do processo que hospeda a sessão, que pode não ser o da janela aberta pelo painel.
- A etapa 4b criou o vínculo manual e o tipo `PedidoVinculo`, que carrega só agente e identificador. As sugestões por nome de pasta foram retiradas porque, sozinho, o nome da pasta não diz a que demanda uma sessão pertence.
- O hook substitui o arquivo de status a cada evento por uma troca atômica que preserva a data de criação do arquivo. Verificado com três eventos em 23 segundos: a data de criação ficou a do primeiro, e só a de modificação mudou.

## Goals / Non-Goals

**Goals:**

- Uma sessão nova do Codex aberta pelo card entra sozinha na demanda e no repositório em que o usuário clicou, com o rótulo informado.
- Nada muda nos scripts instalados nem na configuração de hooks dos agentes.
- O que cruza do arquivo de status para os dados continua sendo só o identificador da sessão e o agente.
- Arquivos de status e de dados já existentes continuam válidos.

**Non-Goals:**

- Vincular automaticamente sessões abertas fora do painel.
- Vincular automaticamente a sessão criada por `/clear` no Claude ou uma segunda conversa no mesmo terminal do Codex.
- Garantir a correspondência certa quando o mesmo repositório recebe lançamentos em duas demandas ao mesmo tempo.

## Decisions

### 1. O painel lembra do clique, em vez de marcar a sessão

Ao abrir uma sessão nova do Codex, o painel grava um lançamento pendente no repositório do card. Quando surge uma sessão que corresponde a ele, o painel a registra ali.

- Por quê: não depende de nada que o Codex faça com os hooks. O lançamento é um dado do usuário principal, gravado por uma ação dele, e é isso que autoriza o vínculo.
- Alternativa descartada: identificador de lançamento levado ao hook por variável de ambiente. Falhou no ensaio com o Codex.
- Alternativa descartada: o hook localizar o lançamento pela cadeia de processos. Depende de qual processo do Codex executa o hook, o que não é garantido, e exigiria reinstalar os scripts.

### 2. Só a sessão nova do Codex gera lançamento

"Nova sessão" do Claude continua registrando a sessão na hora, com o GUID que o painel gera. "Retomar" não gera lançamento, porque a sessão já está registrada.

- Por quê: o lançamento existe para o único caso em que o painel abre uma sessão sem conhecer o identificador.

### 3. Critérios de correspondência

Uma sessão não registrada corresponde a um lançamento quando: o agente é o do lançamento; o identificador é um GUID canônico; o último segmento da pasta de trabalho é o nome do repositório, sem diferenciar maiúsculas; o arquivo de status foi criado na data do lançamento ou depois; o lançamento tem até 24 horas; e a demanda não está arquivada.

- O nome da pasta usa a mesma comparação do aviso de pasta divergente da 4b.
- Por quê o nome da pasta volta a ser critério: sozinho ele gerava ruído, mas aqui ele só vale junto com um clique do usuário naquele repositório e para uma sessão criada depois do clique.

### 4. "Criada depois do clique" vem da data de criação do arquivo

O painel lê a data de criação do arquivo de status no sistema de arquivos e a compara com a data do lançamento.

- Por quê não o campo `atualizado`: ele muda a cada evento. Uma sessão antiga do Codex na mesma pasta seria capturada ao receber uma mensagem depois do clique.
- Por quê não um campo novo gravado pelo hook: exigiria mudar e reinstalar o hook. A data de criação já tem o significado necessário, e a troca atômica do hook a preserva.
- Essa propriedade do hook passa a ser um requisito de `status-de-sessao`, com um caso em `verificar.ps1`, para não ser quebrada por uma mudança futura.
- As duas datas vêm do relógio da mesma máquina.

### 5. Um lançamento, uma sessão

Ao vincular, o painel registra a sessão e remove o lançamento na mesma gravação.

- Por quê: sem etiqueta na sessão, um lançamento que aceitasse várias sessões capturaria qualquer Codex aberto naquela pasta, que é o ruído das sugestões antigas.
- Remover o lançamento ao usá-lo também resolve a remoção: se o usuário remover o registro da sessão, não existe mais lançamento que a traga de volta.
- O rótulo informado em "Nova sessão" fica no lançamento e passa para a sessão.

### 6. Entre vários lançamentos, o mais recente anterior à sessão

As sessões a vincular são tratadas da mais antiga para a mais nova. Cada uma usa, entre os lançamentos pendentes que correspondem, o de data mais recente.

- Por quê: o uso comum é clicar e escrever em seguida, então a sessão nasce logo depois do seu próprio lançamento. Um lançamento abandonado, de uma aba fechada sem mensagem, não captura a sessão de um clique posterior.
- Custo: se o usuário abrir duas abas em demandas diferentes para o mesmo repositório e só depois escrever na primeira, as sessões trocam de demanda. "Mover" corrige.
- Dois lançamentos no mesmo card não têm esse problema: as duas sessões ficam no mesmo card, só os rótulos podem trocar.

### 7. Validade de 24 horas

Um lançamento não usado deixa de valer depois de 24 horas e é removido do arquivo na gravação seguinte de lançamentos naquele repositório.

- Por quê: cobre abrir a aba e só escrever mais tarde no mesmo dia, sem deixar lançamentos abandonados capturando sessões por tempo indefinido.

### 8. Correspondência como função pura, aplicada pelo pedido de vínculo

Uma função do Core recebe as demandas carregadas, as sessões não registradas lidas da pasta de status e o relógio, e devolve os vínculos a aplicar: para cada um, o pedido de vínculo e o lançamento. Uma operação do `DadosPainel` aplica cada vínculo numa única gravação: confere que o lançamento ainda existe e que o identificador não está registrado, registra a sessão com o rótulo do lançamento e `vinculadaEm`, e remove o lançamento.

- Por quê: reaproveita o `PedidoVinculo` da 4b, então o que cruza do arquivo de status continua sendo agente e identificador. A demanda, a pasta e o rótulo vêm do lançamento, que está no arquivo de dados.
- A conferência dentro da gravação cobre duas janelas do painel aplicando o mesmo vínculo: a segunda é recusada.

### 9. Aplicação a cada leitura da pasta de status

Depois de cada retrato da pasta de status, o painel calcula os vínculos a aplicar e, se houver, grava e recarrega os dados antes de montar a tela. Falhas ao gravar são ignoradas: a sessão continua em "Não vinculadas" e é tentada de novo na leitura seguinte.

- Por quê: mantém o caminho único de atualização da tela. A sessão aparece no card no mesmo ciclo em que o arquivo de status surge.
- É a primeira vez que o painel grava sem um clique. A gravação fica restrita ao que a função da decisão 8 devolver.

### 10. O que o vínculo automático passa a permitir

O usuário `ia` escreve os arquivos de status e controla a pasta de trabalho informada e a data de criação do arquivo. Ele pode forjar um arquivo que corresponda a um lançamento pendente.

- **O que um arquivo forjado consegue:** ocupar um lançamento pendente, pondo no card em que o usuário clicou uma sessão com identificador escolhido pelo `ia`. Uma sessão por clique, dentro de 24 horas. A sessão verdadeira fica em "Não vinculadas".
- **O que não consegue:** escolher a demanda ou o repositório, gravar qualquer outro valor no arquivo de dados, ou vincular algo sem que o usuário tenha aberto uma sessão nova do Codex naquele repositório.
- **Efeito ao retomar uma sessão forjada:** o painel executa `codex resume` com o GUID, como `ia`. É o mesmo teto da 4b: algo que o `ia` já pode fazer sozinho.

A regra do projeto muda de "nada entra sem ação do usuário sobre a sessão" para "nada entra fora de um lançamento do usuário". A pasta de trabalho e a data de criação passam a ser comparadas, mas continuam sem ser gravadas e sem compor comandos, argumentos ou caminhos.

### 11. Testes

`verificar.ps1` ganha um caso para o hook: depois de recuar a data de criação de um arquivo de status existente, um novo evento não a altera. No Core: data de criação na leitura; registro, desfazer, carga e poda de lançamentos; abertura do Codex registrando e desfazendo o lançamento; a função de correspondência com cada critério, cada limite e a escolha entre vários lançamentos; a operação de vínculo com o conteúdo gravado; e a aplicação por duas instâncias sobre o mesmo arquivo.

## Risks / Trade-offs

- **Mesmo repositório em duas demandas, com lançamentos pendentes ao mesmo tempo** → a sessão pode cair na outra demanda. A regra do mais recente reduz o caso, e "Mover" corrige.
- **Sessão do Codex aberta à mão na mesma pasta enquanto há um lançamento pendente** → ela pode ocupar o lançamento, e a sessão do painel fica em "Não vinculadas". Limitado às 24 horas do lançamento; "Remover" e o vínculo manual corrigem.
- **Aba aberta e fechada sem mensagem** → o lançamento fica pendente por até 24 horas. A regra do mais recente evita que ele capture a sessão de um clique posterior.
- **Sessão não aparece até a primeira mensagem** → é o comportamento do Codex; o card fica sem a sessão nesse intervalo. Mostrar o lançamento pendente fica como questão em aberto.
- **Gravação sem clique com duas janelas do painel** → a segunda gravação é recusada e ignorada.
- **Uma mudança futura no hook deixar de preservar a data de criação** → coberto pelo caso novo de `verificar.ps1`.
- **O Codex mudar o momento do `SessionStart`** → não afeta a regra: o arquivo continua sendo criado depois do clique.

## Migration Plan

Nada a instalar para o painel funcionar: o `hook.ps1` e o `abrir.ps1` em `C:\Painel` continuam os mesmos. O `verificar.ps1` novo pode ser copiado para `C:\Painel` para rodar a verificação como `ia`.

Sessões abertas antes do painel novo não têm lançamento e continuam dependendo do vínculo manual. Reverter: voltar ao painel anterior, que ignora a lista `lancamentos` e a preserva ao gravar.

## Open Questions

- Mostrar o lançamento pendente no card, com a opção de cancelá-lo. Fica para depois do uso.
- Vincular sozinho a sessão criada por `/clear` no Claude, em que a variável de ambiente pode funcionar. Fica para uma etapa posterior, se fizer falta.
- Indicar na linha da sessão que ela foi vinculada por lançamento.
