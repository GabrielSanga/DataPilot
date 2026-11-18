# DataPilot
Painel de demandas integrado com IA (Claude Code e Codex)

## Painel

Requer o SDK do .NET 10.

```bash
dotnet build
dotnet test
dotnet run --project src/DataPilot.Wpf
```

O painel lê `C:\ProgramData\Painel\sessoes` por padrão e nunca escreve nela.

### Pasta de exemplo e roteiro de conferência

Para ver o painel sem a conta `ia`, gere os dados de exemplo (datas relativas ao momento da geração). O script cria a pasta de status e quatro arquivos de dados, tudo em `$TEMP`:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
```

Para conferir só as sessões lidas da pasta de status (a entrada **Não vinculadas**), use um arquivo de dados que não existe; ele só seria criado numa alteração:

```bash
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-vazio.json"
```

Logo após gerar, a janela deve mostrar:

- Cabeçalho: `2 precisa de atenção · 4 aguardando você · 4 executando`.
- Em **Não vinculadas**: a ordem é precisa de atenção (vermelho), aguardando você (amarelo), executando (azul), ativa (verde), inativa e encerrada (cinza); dentro de cada status, a mais recente primeiro.
- `ex-cwd-longo` com a pasta cortada em 260 caracteres (indicação `…`) e `ex-cwd-controle` sem quebras de linha.
- Não aparecem: os arquivos inválidos (`ex-json-ruim`, `ex-status-ruim`, `ex-id-diferente`, `ex-sem-fuso`, `ex-grande`, `.tmp`).
- Ocultas até marcar "Mostrar antigas (não vinculadas)": `ex-antiga-encerrada` (7 dias) e `ex-antiga-inativa` (4 dias).
- Passar o mouse sobre uma linha mostra a pasta completa e o identificador.

## Demandas, abrir e retomar sessões (Etapas 3, 4a e 4b)

A janela tem duas áreas. À esquerda, a lista de demandas; cada uma mostra a quantidade de repositórios e de sessões e o status mais urgente entre as sessões. A lista é ordenada pelo status mais urgente e, em empate, pela atualização mais recente; demandas sem sessões com status vêm depois, e as arquivadas por último. No fim da lista fica a entrada **Não vinculadas**. À direita, a demanda selecionada: `id`, título, change do OpenSpec (quando houver) e um card por repositório, com as sessões registradas nele.

A demanda e a rolagem escolhidas não mudam sozinhas quando um status muda ou a ordem das demandas muda.

### Ações

- **Nova demanda** (lista): `id` (1 a 30 caracteres de `[A-Za-z0-9_.-]`, nunca só pontos, único sem diferenciar maiúsculas), título (1 a 80 caracteres) e, opcionalmente, a change do OpenSpec (mesma regra de caracteres do `id`). O `id` `sem-demanda` é reservado.
- **Editar**, **Arquivar** e **Desarquivar** (topo da demanda). Arquivar uma demanda com sessão em andamento pede confirmação. **Mostrar arquivadas** (cabeçalho) exibe as arquivadas; elas não oferecem "Nova sessão" nem "Vincular repositório", mas "Retomar" continua disponível.
- A demanda **Sem demanda** guarda os repositórios e as sessões criados antes das demandas: aparece enquanto tiver repositórios e não pode ser editada nem arquivada.
- **Vincular repositório** (topo da demanda): nome da pasta dentro de `C:\Fontes` (só `[A-Za-z0-9_.-]`, nunca só pontos). As pastas já usadas em outras demandas aparecem como sugestão. O mesmo repositório pode estar em várias demandas, cada uma com as próprias sessões. **Remover** (no card) pede confirmação quando há sessões registradas na demanda e não afeta o mesmo repositório em outras demandas.
- **Nova sessão** (no card): escolhe o agente (Claude ou Codex) e um rótulo opcional (até 40 caracteres, sem `;`) e abre uma aba do Windows Terminal que chama o `abrir.ps1` como o usuário `ia`, pelo SSH local. Todas as abas ficam na janela `painel`. O título da aba é `id · repositório [· rótulo]`; para sessões de **Sem demanda**, o nome do agente ocupa o lugar do `id`. A sessão nova do Claude fica registrada no repositório dessa demanda.
- **Retomar**, **Mover** e **Remover** (na linha da sessão registrada). Retomar pede confirmação se a sessão não estiver encerrada, inativa nem sem status. **Mover** leva a sessão para outra demanda, no repositório de mesma pasta (vinculado ao destino se faltar). **Remover** só apaga o registro no painel, depois de confirmar: a conversa e a pasta de status não são tocadas.

Sessões registradas aparecem sempre na sua demanda, sem esconder as antigas; as que não têm arquivo de status aparecem como **sem status** (cinza, por último, e fora do resumo do cabeçalho). As sessões da pasta de status que o painel não registrou (as do Codex e a sessão nova que o Claude cria depois de um `/clear`) ficam em **Não vinculadas**, onde a única ação é **Vincular** (ver abaixo); a opção **Mostrar antigas** vale só para elas (encerradas ou inativas há mais de 3 dias ficam ocultas por padrão).

O comando é montado só com dados do arquivo de dados ou da escolha na tela, nunca com valores lidos de arquivos de status, com uma única exceção: o identificador e o agente de uma sessão que você vincula (ver "Vincular sessões"). O comando é iniciado como lista de argumentos, sem shell. Requer o Windows Terminal (`wt.exe`) e o SSH local da Etapa 1.

### Vincular sessões (Etapa 4b)

As sessões que o painel não abriu (a sessão nova que o Claude cria depois de um `/clear`, uma segunda conversa no mesmo terminal do Codex e as abertas à mão) ficam em **Não vinculadas** até você vinculá-las a uma demanda. A única exceção é a sessão nova do Codex aberta pelo próprio painel, que entra sozinha no card em que você clicou (veja **Vínculo automático do Codex**).

- **Vincular** (linha de **Não vinculadas**): escolha a demanda (não arquivada), o repositório (os da demanda ou um novo, nome de pasta dentro de `C:\Fontes`) e, opcionalmente, um rótulo. A demanda e o repositório vêm pré-selecionados quando o nome do último segmento da pasta de trabalho da sessão coincide com um repositório cadastrado. Se não houver demanda, o painel orienta a criar uma e não grava nada. Se a pasta de trabalho da sessão não coincidir com o repositório escolhido, o painel avisa que a retomada pode não encontrar a conversa e só vincula se você confirmar.
- Os cards de repositório mostram só sessões registradas. Uma sessão não vinculada aparece apenas em **Não vinculadas**, mesmo quando a pasta de trabalho dela tem o nome de um repositório da demanda.
- Só identificadores no formato GUID (8-4-4-4-12) podem ser vinculados; para os demais o botão fica desabilitado, com a dica do motivo.
- Do arquivo de status cruzam para `demandas.json` apenas o **identificador** e o **agente**. A pasta de trabalho, o status, o evento e a data do arquivo de status nunca são gravados. A sessão vinculada ganha o campo `vinculadaEm` (a data do vínculo, que também vale como `criadaEm`); as abertas pelo painel não têm esse campo.
- Vincular de novo uma sessão já registrada (por exemplo, por outra janela do painel) é recusado com uma mensagem.
- **Remover** o registro de uma sessão vinculada a devolve a **Não vinculadas**, enquanto existir arquivo de status para ela.

### Vínculo automático do Codex

O Codex cria o identificador da sessão depois de aberto, então o painel não o conhece ao abrir a aba. Em **Nova sessão** com o Codex, o painel grava no repositório do card um **lançamento pendente** (agente, rótulo e data) em `demandas.json`, antes de abrir o terminal; se o terminal não abrir, o lançamento é desfeito. "Nova sessão" do Claude e **Retomar** não geram lançamento.

O Codex só cria o arquivo de status ao receber a primeira mensagem. Quando surge uma sessão do Codex ainda não registrada, com identificador GUID, cuja pasta de trabalho termina no nome do repositório (sem diferenciar maiúsculas) e cujo arquivo de status foi **criado** na data do lançamento ou depois, o painel a registra naquela demanda e repositório, com o rótulo do lançamento e `vinculadaEm`, sem outra ação. A data de criação do arquivo é lida do sistema de arquivos e o hook a preserva (verificado em `verificar.ps1`).

Limites:

- Cada lançamento vincula **uma** sessão e deixa de existir ao ser usado; por isso uma sessão removida não volta sozinha.
- Um lançamento não usado vale por **24 horas** (e é removido ao gravar outro no mesmo repositório). Demanda arquivada ou repositório removido não vinculam.
- Havendo vários lançamentos pendentes para a mesma pasta, vale o **mais recente** anterior à criação da sessão. Com o mesmo repositório em duas demandas e dois cliques sem mensagem entre eles, as sessões podem cair na demanda trocada; **Mover** corrige.
- Uma sessão antiga do Codex na mesma pasta, criada antes do clique, continua em **Não vinculadas**.
- Uma sessão do Codex aberta à mão na mesma pasta, depois do clique e antes da sessão do painel, pode ocupar o lançamento; **Remover** e o vínculo manual corrigem.
- Continuam manuais: sessões abertas fora do painel, a criada por `/clear` no Claude e a segunda conversa no mesmo terminal do Codex.
- Do arquivo de status só o identificador e o agente são gravados; o nome da pasta e a data de criação do arquivo apenas são comparados com o lançamento, nunca gravados nem usados em comandos.

#### Roteiro do vínculo automático (Etapa 4c)

O script de exemplo também gera `datapilot-exemplo-lancamento.json`: o de várias demandas com um lançamento pendente do Codex (rótulo `ajuste automatico`) no card `Faturamento` de `CHG-142`, de uma hora atrás. A sessão de exemplo do Codex `99999999-aaaa-bbbb-cccc-dddddddddddd` (pasta `Faturamento`) é criada na hora da geração, portanto depois do lançamento. Trabalhe numa cópia, porque o vínculo grava no arquivo:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
cp "$TEMP/datapilot-exemplo-lancamento.json" "$TEMP/datapilot-automatico.json"
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-automatico.json"
```

- Selecione `CHG-142`: a sessão do Codex aparece direto no card `Faturamento`, com o rótulo `ajuste automatico` e o status, e não aparece em **Não vinculadas**. A demanda selecionada não muda.
- No arquivo copiado, o lançamento sumiu e a sessão tem `agente`, `sessionId`, `rotulo`, `criadaEm` e `vinculadaEm`, sem a pasta de trabalho.
- **Remover** a sessão (confirmando): ela volta a **Não vinculadas** e não é vinculada de novo.
- Com `datapilot-exemplo-varias-demandas.json` (sem lançamento) a mesma sessão fica em **Não vinculadas**, como no roteiro da Etapa 4b.

### Sessão ainda sem mensagem (Etapa 4d)

Uma sessão que ainda não recebeu a primeira mensagem se comporta de um jeito diferente em cada agente.

**Codex: a linha "aguardando a primeira mensagem".** Entre o clique em **Nova sessão** e a primeira mensagem, o Codex não cria arquivo de status. Nesse intervalo o card mostra uma linha para o lançamento pendente, depois das sessões registradas, com o agente, o rótulo (quando houver), o texto "aguardando a primeira mensagem" e o tempo decorrido. Com vários lançamentos, o mais recente vem primeiro.

- A linha não é uma sessão: só tem **Abrir** e **Cancelar** (sem **Retomar**, **Mover** nem **Remover**). Ela não entra na quantidade de sessões da demanda, no status mais urgente, na ordem da lista nem no resumo do cabeçalho.
- Quando a primeira mensagem chega e o vínculo automático acontece, a linha dá lugar à sessão na mesma atualização da tela.
- Lançamentos vencidos (mais de 24 horas) e os de demanda arquivada não são mostrados.
- **Abrir** abre o Codex de novo no mesmo repositório, com o rótulo do lançamento. Serve para a aba fechada antes da primeira mensagem: como ainda não existe conversa, é o equivalente a retomar. O lançamento é reaproveitado, então o card continua com uma linha só, e a data dele passa a ser a desse clique (as 24 horas recomeçam). Se a aba anterior ainda estiver aberta, a primeira das duas a receber mensagem fica no card e a outra vai para **Não vinculadas**.
- **Cancelar** remove só aquele lançamento de `demandas.json`, sem confirmar e sem fechar a aba. Se a aba continuar aberta e receber uma mensagem, a sessão vai para **Não vinculadas** e pode ser vinculada à mão. Cancelar um lançamento que outra janela do painel acabou de usar não altera nada.

**Claude: retomar uma sessão fechada antes da primeira mensagem.** O Claude só grava a conversa depois da primeira mensagem, então `claude --resume` não encontra uma sessão fechada antes disso e termina com erro. O `abrir.ps1` trata esse caso: se a retomada do Claude terminar com código diferente de 0 em menos de 30 segundos, ele avisa "A conversa ainda nao tem mensagens" e inicia o Claude com `--session-id` e o mesmo identificador, de modo que o registro e o arquivo de status continuam valendo. Uma retomada que falha depois de 30 segundos e a retomada do Codex nunca têm segunda tentativa. O comando montado pelo painel não muda; basta copiar o `abrir.ps1` novo para `C:\Painel` (veja `docs/instalacao-etapa-1.md`).

#### Roteiro da linha do lançamento pendente (Etapa 4d)

O script de exemplo também gera `datapilot-exemplo-aguardando.json`: o de várias demandas com dois lançamentos pendentes do Codex no card `Estoque` de `CHG-142`, um de 5 minutos atrás (rótulo `aguardando exemplo`) e um de 3 horas atrás (sem rótulo). Não há sessão do Codex na pasta `Estoque`, então nada os vincula. Trabalhe numa cópia, porque **Cancelar** grava no arquivo:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
cp "$TEMP/datapilot-exemplo-aguardando.json" "$TEMP/datapilot-aguardando.json"
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-aguardando.json"
```

- Selecione `CHG-142`: o card `Estoque` mostra as duas sessões e, depois delas, duas linhas do Codex "aguardando a primeira mensagem" (`5 min` com o rótulo `aguardando exemplo`, depois `3 h`), cada uma com **Abrir** e **Cancelar**. O resumo da demanda continua `2 repositórios · 3 sessões · precisa de atenção` e o do cabeçalho não muda.
- **Cancelar** a linha de `5 min`: só ela some, a de `3 h` continua, `CHG-142` continua selecionada e o arquivo copiado perde só aquele lançamento.
- Para ver a substituição pela sessão, com o painel aberto crie na pasta de status um arquivo de uma sessão do Codex com `cwd` `C:\Fontes\Estoque` (GUID novo, `status` `ativa`, `atualizado` agora): a linha restante é substituída pela sessão, sem o lançamento no arquivo, e a demanda selecionada não muda.
- Com `datapilot-exemplo-varias-demandas.json` (sem lançamento) o card `Estoque` não mostra nenhuma linha de lançamento.

#### Roteiro da retomada sem mensagem (Etapa 4d)

1. Copie `abrir.ps1` e `verificar.ps1` para `C:\Painel`, como administrador, e rode a verificação como `ia` (`docs/instalacao-etapa-1.md`, passos 2 e 3): a última linha deve terminar com `Falhas: 0`.
2. No painel, abra uma sessão **nova do Claude** pelo card, feche a aba **sem digitar nada** (não use `/exit`) e acione **Retomar**: o terminal avisa que a conversa ainda não tem mensagens, o Claude abre com o mesmo identificador, o status volta a `ativa` e a mesma linha do card continua valendo.
3. Numa sessão do Claude com mensagens, feche a aba e acione **Retomar**: a conversa volta com o histórico e não há aviso.

### Progresso do OpenSpec (Etapa 5a)

Quando a demanda selecionada tem `openspecChange`, o painel lê o `tasks.md` da change em cada repositório da demanda, em `C:\Fontes\<pasta>\openspec\changes\<openspecChange>\tasks.md`, e mostra:

- abaixo do nome da change, uma barra com a soma dos repositórios em que a change foi encontrada ("7 de 12 tarefas");
- em cada card, o progresso daquele repositório ("4 de 6");
- "arquivada" quando só existe `openspec\changes\archive\AAAA-MM-DD-<change>` (a mais recente; a change ativa tem prioridade);
- "change não encontrada nos repositórios", "sem tarefas" (nenhuma linha `- [ ]`/`- [x]`) e "progresso indisponível" (arquivo recusado, acima de 256 KB ou com UTF-8 inválido; esse repositório fica fora da soma).

São contadas as linhas `- [ ]`, `- [x]` e `- [X]` (com indentação). O progresso é relido a cada atualização da tela (até 5 segundos) e ao trocar de demanda; as pastas do `ia` não são vigiadas. O `tasks.md` é só lido, nunca gravado, e o painel recusa atalhos e junções em qualquer trecho do caminho. Para usar outra raiz de fontes, sem tocar em `C:\Fontes`:

```bash
dotnet run --project src/DataPilot.Wpf -- --raiz-fontes "$TEMP/datapilot-exemplo-fontes"
```

#### Roteiro do progresso (Etapa 5a)

1. Gere os exemplos: `./scripts/exemplos/gerar-sessoes-exemplo.ps1`. Ele cria a raiz `datapilot-exemplo-fontes` (com uma change ativa, uma arquivada e uma junção) e o arquivo `datapilot-exemplo-progresso.json`.
2. Abra o painel:

```bash
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-exemplo-progresso.json" --raiz-fontes "$TEMP/datapilot-exemplo-fontes"
```

3. **CHG-142**: a barra mostra "7 de 12 tarefas"; **Faturamento** mostra "4 de 6" e **Estoque** "3 de 6".
4. **CHG-150**: a barra mostra "2 de 2 tarefas · arquivada"; **Faturamento** mostra "2 de 2 · arquivada", **Compras** "progresso indisponível" (a pasta da change é uma junção), **Portal** "sem tarefas" e **Estoque** nada.
5. **CHG-160**: a área mostra "change não encontrada nos repositórios" e nenhum card mostra progresso.
6. Com **CHG-142** selecionada, troque um `- [ ]` por `- [x]` em `datapilot-exemplo-fontes\Faturamento\openspec\changes\ajuste-nfe\tasks.md`: em até 5 segundos a barra passa a "8 de 12 tarefas" e o card a "5 de 6", sem mudar a demanda selecionada.

Verificação com a conta `ia` (junção, arquivamento e privilégio de link simbólico): tarefas 3.1 a 3.4 de `openspec/changes/etapa-5a-progresso-openspec/tasks.md`.

### Arquivo de dados

O painel guarda demandas, repositórios e sessões abertas por ele em `%APPDATA%\Painel\demandas.json`. O arquivo só é criado na primeira alteração. O arquivo gravado pela Etapa 3 (só com `sem-demanda`) é lido sem conversão; recomenda-se copiá-lo antes do primeiro uso da versão nova. Se existir e não puder ser lido, o painel mostra um aviso com o caminho, não grava nada e desabilita criar e alterar demandas, abrir e retomar (a lista continua funcionando). Para usar outro arquivo, sem tocar nos dados reais:

```bash
dotnet run --project src/DataPilot.Wpf -- --dados "$TEMP/meus-dados.json"
```

Ao carregar, entradas inválidas são desconsideradas: demanda com `id` ou título fora das regras ou com `id` repetido (vale a primeira), repositório com pasta inválida ou repetida na mesma demanda, sessão com identificador que não seja GUID ou agente desconhecido, e identificador de sessão repetido no arquivo (vale a primeira). Um `openspecChange` inválido é tratado como ausente.

### Roteiro de verificação

Com os dois arquivos gerados pelo script (`datapilot-exemplo` e `datapilot-exemplo-varias-demandas.json`):

```bash
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-exemplo-varias-demandas.json"
```

- O cabeçalho mostra `2 precisa de atenção · 4 aguardando você · 4 executando` (as sessões registradas e as não vinculadas contam juntas; "sem status" não conta). A lista mostra, nesta ordem: `CHG-142` (precisa de atenção), `CHG-150` (aguardando você), **Sem demanda** (ativa), `CHG-200` (0 repositórios, sem status) e **Não vinculadas**. `CHG-099` só aparece com **Mostrar arquivadas**, por último e marcada como arquivada.
- Em `CHG-142`: dois cards, `Faturamento` e `Estoque`; em `Estoque`, a sessão "ajuste de saldo" (executando) vem antes da "sem arquivo de status" (**sem status**, cinza, com **Retomar**).
- `Faturamento` aparece em `CHG-142`, `CHG-150` e **Sem demanda**, cada um com a própria sessão.
- Em **Sem demanda**, a sessão encerrada tem **Retomar**, e a linha mostra a pasta `C:\Windows` do arquivo de status, mas o repositório de abertura é `Faturamento` (vale o registrado).
- Em `CHG-200`, a área da demanda orienta a vincular um repositório e não há cards.
- Em **Não vinculadas**, as linhas só têm **Vincular** (desabilitado quando o identificador não é GUID); não há **Retomar**.
- Para ver o aviso de dados indisponíveis, aponte `--dados` para um arquivo com texto que não seja JSON: **Nova demanda** e as ações das demandas ficam desabilitadas e o arquivo não é alterado.
- Para ver o estado de nenhuma demanda, aponte `--dados` para um arquivo que não existe: a janela orienta a criar uma demanda e só mostra **Não vinculadas**.

#### Roteiro do vínculo (Etapa 4b)

O script de exemplo cria uma sessão do Codex não vinculada (`99999999-aaaa-bbbb-cccc-dddddddddddd`, pasta `C:\Fontes\Faturamento`). Trabalhe numa cópia do arquivo de dados, porque o vínculo grava nele:

```bash
cp "$TEMP/datapilot-exemplo-varias-demandas.json" "$TEMP/datapilot-vinculo.json"
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-vinculo.json"
```

- Em `CHG-142`, `CHG-150` e **Sem demanda**, o card `Faturamento` não mostra a sessão do Codex: ela aparece só em **Não vinculadas**.
- Em **Não vinculadas**, use **Vincular** na sessão do Codex: o diálogo já vem com uma demanda que tem `Faturamento` e com esse repositório. Escolha `CHG-142` e confirme: a sessão passa a aparecer no card `Faturamento` de `CHG-142`, com o status, e some de **Não vinculadas**. No arquivo, o registro tem `agente`, `sessionId`, `criadaEm` e `vinculadaEm`, e nada da pasta de trabalho.
- Em **Não vinculadas**, **Vincular** nas sessões de exemplo cujo identificador não é GUID (`ex-atencao`, `ex-aguardando-1`, ...) está desabilitado, com a dica do motivo.
- Em **Não vinculadas**, use **Vincular** numa sessão com GUID (por exemplo `22222222-3333-4444-5555-666666666666`, do Claude em `Estoque`): o diálogo já vem com `CHG-142` e `Estoque`. Escolha `Faturamento`: o painel avisa; cancele no aviso e o diálogo continua aberto sem gravar nada.
- **Remover** a sessão vinculada (confirmando): ela volta a **Não vinculadas**.
- Sem nenhuma demanda não arquivada (use `--dados` com um arquivo inexistente), **Vincular** orienta a criar uma demanda e não grava nada.

O primeiro arquivo de exemplo (`datapilot-exemplo-demandas.json`, no formato da Etapa 3, só com **Sem demanda**) serve para conferir que o arquivo antigo continua sendo lido.

Verificação manual com a conta `ia` (grupo 6 de `openspec/changes/etapa-4a-demandas/tasks.md`):

1. Copie o `demandas.json` real, abra o painel novo com `--dados` apontando para a cópia e confira que **Sem demanda** traz os repositórios e as sessões de antes, e que **Retomar** abre a conversa certa.
2. Crie uma demanda, vincule dois repositórios que existam em `C:\Fontes` e abra uma sessão do Claude em cada um: elas aparecem agrupadas, o título de cada aba começa pelo `id` da demanda e o resumo na lista acompanha o status.
3. Mova uma sessão de **Sem demanda** para a demanda criada e retome-a: a conversa certa abre.
4. Execute `/clear` numa sessão do Claude e abra uma sessão do Codex à mão, fora do painel: as duas sessões novas aparecem em **Não vinculadas**, com a ação **Vincular**. (O Codex aberto pelo card entra sozinho: veja **Vínculo automático do Codex**.)
5. Com uma demanda selecionada e sessões em andamento, use o painel por alguns minutos: a seleção e a rolagem não mudam sozinhas. Depois arquive e desarquive a demanda.
6. Confirme que o `ia` não lê o arquivo de dados (deve ser negado), executando como usuário principal: `ssh ia@localhost powershell -NoProfile -Command Get-Content $env:APPDATA\Painel\demandas.json`.
