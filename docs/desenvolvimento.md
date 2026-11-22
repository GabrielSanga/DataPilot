# Desenvolvimento e verificação

Guia técnico do DataPilot (o mapa do código está em [arquitetura.md](arquitetura.md)): como compilar, gerar os dados de exemplo e conferir cada funcionalidade. Os comandos são executados na raiz do repositório. Para a visão geral do produto, veja o [README](../README.md).

## Painel

Requer o SDK do .NET 10.

```bash
dotnet build
dotnet test
dotnet run --project src/DataPilot.Wpf
```

O painel lê `C:\ProgramData\Painel\sessoes` por padrão e nunca escreve nela.

### Nomes na tela e no arquivo

Na tela, o que o arquivo de dados, o código e as specs chamam de **demanda** é o **trabalho**, e a entrada que reúne as sessões fora de trabalhos é **Sessões** (a demanda fixa `sem-demanda`, que o painel cria sozinho quando precisa). Neste README, "trabalho" e "Sessões" são os nomes da tela; `demandas.json`, `sem-demanda` e `openspecChange` são os do arquivo.

### Pasta de exemplo e roteiro de conferência

Para ver o painel sem a conta `ia`, gere os dados de exemplo (datas relativas ao momento da geração). O script cria a pasta de status, uma raiz de fontes de exemplo e os arquivos de dados, tudo em `$TEMP`:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
```

Para conferir só as sessões lidas da pasta de status (a entrada **Sessões**), use um arquivo de dados que não existe; ele só seria criado numa alteração:

```bash
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-vazio.json"
```

Logo após gerar, a janela deve mostrar:

- Barra superior: o alerta vermelho `2 precisam de atenção`, e ao lado, em texto, `4 aguardando você` e `4 executando`.
- Em **Sessões**: a ordem é precisa de atenção (vermelho), aguardando você (amarelo), executando (azul), ativa (verde), inativa e encerrada (cinza); dentro de cada status, a mais recente primeiro.
- `ex-cwd-longo` com a pasta cortada em 260 caracteres (indicação `…`) e `ex-cwd-controle` sem quebras de linha.
- Não aparecem: os arquivos inválidos (`ex-json-ruim`, `ex-status-ruim`, `ex-id-diferente`, `ex-sem-fuso`, `ex-grande`, `.tmp`).
- Ocultas até marcar **Mostrar antigas** (dentro de **Sessões**): `ex-antiga-encerrada` (7 dias) e `ex-antiga-inativa` (4 dias).
- Passar o mouse sobre uma linha mostra o status, a pasta completa e o identificador.

## Trabalhos, Sessões, abrir e retomar

### Organização da tela

A janela segue a ideia do "cockpit escuro": fica calma por padrão e só acende o que precisa de você. Tem três regiões.

- **Barra superior:** o nome do produto e o **alerta**. Com sessões em "precisa de atenção", o alerta é uma faixa vermelha (`2 precisam de atenção`); sem nenhuma e com sessões em "aguardando você", é amarela (`1 aguardando você`); sem nenhuma das duas, mostra só o texto `Nada precisa de você agora`. Ao lado, em texto, as contagens de "aguardando você" e "executando". O alerta conta as sessões dos trabalhos listados e as de **Sessões** visíveis; "sem status" e as sessões excluídas não contam. Ele acende com um esmaecimento de 150 ms, uma vez (sem animação se as animações do Windows estiverem desligadas).
- **Lista (à esquerda):** **Novo trabalho**, o filtro, os trabalhos e, no pé, **Mostrar arquivados**. Cada item mostra o título e as contagens em frase (`2 repositórios, 3 sessões`); o identificador do trabalho nunca aparece. O trabalho com sessão que precisa de você ganha uma faixa de 4 px na cor do status mais urgente e o nome dele. A lista é ordenada pelo status mais urgente e, em empate, pela atualização mais recente; trabalhos sem sessões com status vêm depois, e os arquivados por último. **Sessões** fica separada por uma linha, no fim.
- **Item selecionado (à direita):** num trabalho, o título, **Vincular repositório** e um card por repositório: cabeçalho com o nome, a **change do OpenSpec** daquele repositório, a barra de progresso e **Nova sessão**, sobre uma linha grafite, e abaixo as linhas das sessões. Em **Sessões**, **Nova sessão**, **Mostrar antigas** e as linhas das sessões. Com nada selecionado, só aparece a orientação. Erros e mensagens de operação aparecem numa faixa no topo desta área, com um botão para fechá-la.

A **linha da sessão** mostra, nesta ordem, um indicador redondo na cor do status (vermelho: precisa de atenção; amarelo: aguardando você; azul: executando; verde: ativa; cinza: inativa, encerrada e sem status), o agente, o rótulo (em branco quando não há), o tempo decorrido e as ações. O nome do status não aparece na linha: fica na dica (ao passar o mouse) e no nome acessível do indicador; com o **alto contraste** do Windows ligado, ele volta a aparecer em texto na linha. Só as sessões em "precisa de atenção" e "aguardando você" ganham a faixa colorida à esquerda.

O trabalho selecionado e a rolagem não mudam sozinhos quando um status muda ou a ordem dos trabalhos muda, nem quando você filtra.

### Alerta e filtro

- **Filtro** (**Ctrl+F** leva o foco ao campo; **Esc** limpa o texto): compara com o título, os nomes dos repositórios e as changes deles, sem diferenciar maiúsculas nem acentos (`relatorio` acha "Relatório mensal"). **Sessões** sempre continua na lista, e filtrar não muda o item selecionado.

### Menus "Mais ações" e atalhos

Cada linha e cada bloco mostra só a ação principal; o resto fica no menu "Mais ações" (botão `…`, o botão direito, a tecla de menu ou **Shift+F10** sobre a linha).

| Onde | Ações visíveis | No menu |
|---|---|---|
| Sessão de um trabalho | Retomar, Excluir (lixeira) | (sem menu) |
| Sessão em Sessões | Retomar, Excluir | Vincular a um trabalho |
| Lançamento pendente | Abrir | Cancelar |
| Repositório | Nova sessão | Trocar change, Remover repositório |
| Trabalho | Vincular repositório | Editar, Arquivar ou Desarquivar |
| Sessões | Nova sessão | (sem menu) |

Atalhos: **Ctrl+N** (novo trabalho), **Ctrl+F** (filtro). O foco do teclado sempre aparece como um contorno grafite de 2 px. Os diálogos não têm texto de ajuda permanente: **Enter** aciona o botão principal (que leva o nome da ação) e **Esc** cancela; uma restrição violada aparece como erro junto do campo, sem fechar o diálogo nem apagar o que foi digitado. As confirmações (excluir sessão, remover repositório, arquivar com sessão em andamento, abrir outra aba de uma sessão possivelmente aberta) são do próprio painel, têm o botão nomeado pela ação e **Cancelar** como padrão, então Enter não confirma uma exclusão.

Com o **alto contraste** do Windows ligado, a janela e os diálogos passam a usar as cores do sistema e cada sessão mostra o nome do status em texto. Os títulos e as contagens usam a fonte Bahnschrift (que acompanha o Windows 10); sem ela, o texto cai para a Segoe UI.

### Ações

- **Novo trabalho** (lista, **Ctrl+N**): só o título (1 a 80 caracteres). O painel gera o identificador interno (`t` e 8 dígitos hexadecimais), que não aparece na tela. **Editar** (menu do trabalho) altera só o título.
- **Arquivar** e **Desarquivar** (menu do trabalho). Arquivar um trabalho com sessão em andamento pede confirmação. **Mostrar arquivados** (pé da lista) exibe os arquivados; eles não oferecem "Nova sessão" nem "Vincular repositório", mas "Retomar" continua disponível. **Sessões** não pode ser editada nem arquivada.
- **Vincular repositório** (topo do trabalho): abre a escolha de pasta do Windows, já em `C:\Fontes`. Só uma pasta **direta** de `C:\Fontes`, com nome `[A-Za-z0-9_.-]` (nunca só pontos), é aceita; outra pasta mostra o erro e nada é vinculado (**Escolher pasta…** tenta de novo; cancelar a escolha fecha o diálogo). Escolhida a pasta, o diálogo lista as **changes ativas** do repositório (`openspec\changes`, sem `archive`) e **Nenhuma**; a escolhida fica gravada no repositório. O mesmo repositório pode estar em vários trabalhos, cada um com as próprias sessões e a própria change. **Remover repositório** (menu do repositório) pede confirmação quando há sessões registradas no trabalho e não afeta o mesmo repositório em outros trabalhos.
- **Trocar change** (menu do repositório): a mesma lista de changes, para o repositório já vinculado; só o repositório é alterado.
- **Nova sessão** (cabeçalho do repositório): o diálogo pede só o agente (Claude ou Codex) e um rótulo opcional (até 40 caracteres, sem `;`) e abre uma aba do Windows Terminal que chama o `abrir.ps1` como o usuário `ia`, pelo SSH local. Todas as abas ficam na janela `painel`. O título da aba é `título do trabalho · repositório [· rótulo]`: o título entra com até 40 caracteres e cada `;` vira `,` (o Windows Terminal trata `;` como separador de comandos). Para sessões de **Sessões**, o nome do agente ocupa o lugar do título. A sessão nova do Claude fica registrada no repositório desse trabalho.
- **Retomar** (linha da sessão). Pede confirmação se a sessão não estiver encerrada, inativa nem sem status.
- **Excluir** (lixeira na linha de toda sessão): depois de confirmar, tira a sessão do painel de vez. O registro sai de `demandas.json` e o identificador entra na lista `excluidas`; ela não aparece mais em lugar nenhum nem é vinculada sozinha. A conversa e a pasta de status não são tocadas. Não há "desfazer" na tela: para trazer a sessão de volta, apague o item de `excluidas` no arquivo.

Sessões registradas aparecem sempre no seu trabalho, sem esconder as antigas; as que não têm arquivo de status aparecem com o indicador cinza (sem status; ficam por último e fora do alerta do topo).

O comando é montado só com dados do arquivo de dados ou da escolha na tela, nunca com valores lidos de arquivos de status, com uma única exceção: o identificador e o agente de uma sessão que você retoma ou vincula a partir de **Sessões** (ver abaixo). O comando é iniciado como lista de argumentos, sem shell. Requer o Windows Terminal (`wt.exe`) e o SSH local da Etapa 1.

### Sessões

**Sessões** reúne as sessões que não estão em nenhum trabalho: as abertas sem trabalho (**Nova sessão** nessa área), as que o painel não abriu (as do Codex e a sessão nova que o Claude cria depois de um `/clear`) e os lançamentos pendentes delas. Cada linha mostra o repositório. A opção **Mostrar antigas** fica nessa área e vale só para as sessões não registradas (encerradas ou inativas há mais de 3 dias ficam ocultas por padrão).

- **Nova sessão** (topo da área): abre a escolha de pasta do Windows (mesma regra: só pasta direta de `C:\Fontes`), depois o diálogo do agente e do rótulo. A pasta é vinculada a **Sessões**, se ainda não estiver, e a sessão abre e fica registrada nela.
- **Retomar** numa sessão registrada em Sessões: abre no repositório do registro. Numa sessão que o painel não registrou, o painel pergunta o **repositório** entre as pastas diretas de `C:\Fontes` (vem pré-selecionada a de mesmo nome que o último segmento da pasta de trabalho da sessão), registra a sessão em Sessões, nesse repositório, com `vinculadaEm`, e abre a conversa. Se o repositório escolhido não coincide com a pasta de trabalho, o painel avisa que a retomada pode não encontrar a conversa e só segue se você confirmar. Só identificadores no formato GUID (8-4-4-4-12) podem ser retomados ou vinculados; para os demais os botões ficam desabilitados, com o motivo na própria linha, e **Excluir** continua disponível.
- **Vincular a um trabalho** (menu da linha): escolha o trabalho (não arquivado), o repositório (os do trabalho ou um novo) e, opcionalmente, um rótulo. O trabalho e o repositório vêm pré-selecionados quando o nome do último segmento da pasta de trabalho coincide com um repositório cadastrado. Uma sessão já registrada em Sessões é **movida** para o trabalho escolhido (o identificador passa a constar só lá); uma não registrada é registrada nele. Se não houver trabalho, o painel orienta a criar um e não grava nada.
- Do arquivo de status cruzam para `demandas.json` apenas o **identificador** e o **agente**: ao retomar ou vincular uma sessão, por correspondência com um lançamento e ao **excluir** (aí vale a regra do hook, `[A-Za-z0-9_-]` até 100 caracteres, e o identificador só é comparado). A pasta de trabalho, o status, o evento e a data do arquivo de status nunca são gravados.

### Vínculo automático do Codex

O Codex cria o identificador da sessão depois de aberto, então o painel não o conhece ao abrir a aba. Em **Nova sessão** com o Codex, o painel grava no repositório do card um **lançamento pendente** (agente, rótulo e data) em `demandas.json`, antes de abrir o terminal; se o terminal não abrir, o lançamento é desfeito. "Nova sessão" do Claude e **Retomar** não geram lançamento.

O Codex só cria o arquivo de status ao receber a primeira mensagem. Quando surge uma sessão do Codex ainda não registrada, com identificador GUID, cuja pasta de trabalho termina no nome do repositório (sem diferenciar maiúsculas) e cujo arquivo de status foi **criado** na data do lançamento ou depois, o painel a registra naquele trabalho e repositório, com o rótulo do lançamento e `vinculadaEm`, sem outra ação. A data de criação do arquivo é lida do sistema de arquivos e o hook a preserva (verificado em `verificar.ps1`). Sessões excluídas nunca são vinculadas.

Limites:

- Cada lançamento vincula **uma** sessão e deixa de existir ao ser usado; por isso uma sessão excluída não volta sozinha.
- Um lançamento não usado vale por **24 horas** (e é removido ao gravar outro no mesmo repositório). Trabalho arquivado ou repositório removido não vinculam.
- Havendo vários lançamentos pendentes para a mesma pasta, vale o **mais recente** anterior à criação da sessão. Com o mesmo repositório em dois trabalhos e dois cliques sem mensagem entre eles, as sessões podem cair no trabalho trocado; exclua a sessão e abra outra, ou vincule à mão.
- Uma sessão antiga do Codex na mesma pasta, criada antes do clique, continua em **Sessões**.
- Uma sessão do Codex aberta à mão na mesma pasta, depois do clique e antes da sessão do painel, pode ocupar o lançamento; **Excluir** e o vínculo manual corrigem.
- Continuam manuais: sessões abertas fora do painel, a criada por `/clear` no Claude e a segunda conversa no mesmo terminal do Codex.
- Do arquivo de status só o identificador e o agente são gravados; o nome da pasta e a data de criação do arquivo apenas são comparados com o lançamento, nunca gravados nem usados em comandos.

#### Roteiro do vínculo automático (Etapa 4c)

O script de exemplo também gera `datapilot-exemplo-lancamento.json`: o de vários trabalhos com um lançamento pendente do Codex (rótulo `ajuste automatico`) no card `Faturamento` de `CHG-142` ("Ajuste NF-e"), de uma hora atrás. A sessão de exemplo do Codex `99999999-aaaa-bbbb-cccc-dddddddddddd` (pasta `Faturamento`) é criada na hora da geração, portanto depois do lançamento. Trabalhe numa cópia, porque o vínculo grava no arquivo:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
cp "$TEMP/datapilot-exemplo-lancamento.json" "$TEMP/datapilot-automatico.json"
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-automatico.json"
```

- Selecione "Ajuste NF-e": a sessão do Codex aparece direto no card `Faturamento`, com o rótulo `ajuste automatico` e o status, e não aparece em **Sessões**. O trabalho selecionado não muda.
- No arquivo copiado, o lançamento sumiu e a sessão tem `agente`, `sessionId`, `rotulo`, `criadaEm` e `vinculadaEm`, sem a pasta de trabalho.
- **Excluir** a sessão (lixeira da linha, confirmando): ela some do painel e não é vinculada de novo.
- Com `datapilot-exemplo-varias-demandas.json` (sem lançamento) a mesma sessão fica em **Sessões**.

### Sessão ainda sem mensagem (Etapa 4d)

Uma sessão que ainda não recebeu a primeira mensagem se comporta de um jeito diferente em cada agente.

**Codex: a linha "aguardando a primeira mensagem".** Entre o clique em **Nova sessão** e a primeira mensagem, o Codex não cria arquivo de status. Nesse intervalo o card mostra uma linha para o lançamento pendente, depois das sessões registradas, com o agente, o rótulo (quando houver), o texto "aguardando a primeira mensagem" e o tempo decorrido. Com vários lançamentos, o mais recente vem primeiro.

- A linha não é uma sessão: só tem **Abrir** e, no menu "Mais ações", **Cancelar** (sem **Retomar** nem **Excluir**). Ela não entra na quantidade de sessões do trabalho, no status mais urgente, na ordem da lista nem no alerta do topo.
- Quando a primeira mensagem chega e o vínculo automático acontece, a linha dá lugar à sessão na mesma atualização da tela.
- Lançamentos vencidos (mais de 24 horas) e os de trabalho arquivado não são mostrados.
- **Abrir** abre o Codex de novo no mesmo repositório, com o rótulo do lançamento. Serve para a aba fechada antes da primeira mensagem: como ainda não existe conversa, é o equivalente a retomar. O lançamento é reaproveitado, então o card continua com uma linha só, e a data dele passa a ser a desse clique (as 24 horas recomeçam). Se a aba anterior ainda estiver aberta, a primeira das duas a receber mensagem fica no card e a outra vai para **Sessões**.
- **Cancelar** remove só aquele lançamento de `demandas.json`, sem confirmar e sem fechar a aba. Se a aba continuar aberta e receber uma mensagem, a sessão vai para **Sessões** e pode ser vinculada à mão. Cancelar um lançamento que outra janela do painel acabou de usar não altera nada.

**Claude: retomar uma sessão fechada antes da primeira mensagem.** O Claude só grava a conversa depois da primeira mensagem, então `claude --resume` não encontra uma sessão fechada antes disso e termina com erro. O `abrir.ps1` trata esse caso: se a retomada do Claude terminar com código diferente de 0 em menos de 30 segundos, ele avisa "A conversa ainda nao tem mensagens" e inicia o Claude com `--session-id` e o mesmo identificador, de modo que o registro e o arquivo de status continuam valendo. Uma retomada que falha depois de 30 segundos e a retomada do Codex nunca têm segunda tentativa. O comando montado pelo painel não muda; basta copiar o `abrir.ps1` novo para `C:\Painel` (veja `docs/instalacao-etapa-1.md`).

#### Roteiro da linha do lançamento pendente (Etapa 4d)

O script de exemplo também gera `datapilot-exemplo-aguardando.json`: o de vários trabalhos com dois lançamentos pendentes do Codex no card `Estoque` de `CHG-142`, um de 5 minutos atrás (rótulo `aguardando exemplo`) e um de 3 horas atrás (sem rótulo). Não há sessão do Codex na pasta `Estoque`, então nada os vincula. Trabalhe numa cópia, porque **Cancelar** grava no arquivo:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
cp "$TEMP/datapilot-exemplo-aguardando.json" "$TEMP/datapilot-aguardando.json"
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-aguardando.json"
```

- Selecione "Ajuste NF-e": o card `Estoque` mostra as duas sessões e, depois delas, duas linhas do Codex "aguardando a primeira mensagem" (`5 min` com o rótulo `aguardando exemplo`, depois `3 h`), cada uma com **Abrir** e, no menu "Mais ações", **Cancelar**. O resumo do trabalho continua `2 repositórios, 3 sessões` e o alerta do topo não muda.
- **Cancelar** (menu da linha) a linha de `5 min`: só ela some, a de `3 h` continua, o trabalho continua selecionado e o arquivo copiado perde só aquele lançamento.
- Para ver a substituição pela sessão, com o painel aberto crie na pasta de status um arquivo de uma sessão do Codex com `cwd` `C:\Fontes\Estoque` (GUID novo, `status` `ativa`, `atualizado` agora): a linha restante é substituída pela sessão, sem o lançamento no arquivo, e o trabalho selecionado não muda.
- Com `datapilot-exemplo-varias-demandas.json` (sem lançamento) o card `Estoque` não mostra nenhuma linha de lançamento.

#### Roteiro da retomada sem mensagem (Etapa 4d)

1. Copie `abrir.ps1` e `verificar.ps1` para `C:\Painel`, como administrador, e rode a verificação como `ia` (`docs/instalacao-etapa-1.md`, passos 2 e 3): a última linha deve terminar com `Falhas: 0`.
2. No painel, abra uma sessão **nova do Claude** pelo card, feche a aba **sem digitar nada** (não use `/exit`) e acione **Retomar**: o terminal avisa que a conversa ainda não tem mensagens, o Claude abre com o mesmo identificador, o status volta a `ativa` e a mesma linha do card continua valendo.
3. Numa sessão do Claude com mensagens, feche a aba e acione **Retomar**: a conversa volta com o histórico e não há aviso.

### Change e progresso do OpenSpec

Cada **repositório** de um trabalho pode ter a própria change do OpenSpec (campo `openspecChange` do repositório, escolhido em **Vincular repositório** ou **Trocar change**). Para cada repositório com change, o painel lê o `tasks.md` em `C:\Fontes\<pasta>\openspec\changes\<change>\tasks.md` e mostra no card dele o nome da change, uma barra e o progresso:

- "4 de 6" (tarefas concluídas e total); não há soma no topo do trabalho, porque cada repositório tem a própria change;
- "arquivada" quando só existe `openspec\changes\archive\AAAA-MM-DD-<change>` (a mais recente; a change ativa tem prioridade);
- "change não encontrada" (a pasta da change não existe no repositório), "sem tarefas" (nenhuma linha `- [ ]`/`- [x]`) e "progresso indisponível" (arquivo recusado, acima de 256 KB ou com UTF-8 inválido), cada uma só no card do repositório afetado.

São contadas as linhas `- [ ]`, `- [x]` e `- [X]` (com indentação). O progresso do trabalho selecionado é relido a cada atualização da tela (até 5 segundos), ao trocar de trabalho e ao trocar a change; as pastas do `ia` não são vigiadas. O `tasks.md` é só lido, nunca gravado, e o painel recusa atalhos e junções em qualquer trecho do caminho. A lista de changes oferecida ao vincular lê os nomes das pastas de `openspec\changes` (só pastas comuns, nome válido, até 200, em ordem alfabética, sem `archive`); os nomes só são exibidos e, escolhido um, passam pela mesma validação antes de gravar. Para usar outra raiz de fontes, sem tocar em `C:\Fontes` (vale também para a escolha de pasta):

```bash
dotnet run --project src/DataPilot.Wpf -- --raiz-fontes "$TEMP/datapilot-exemplo-fontes"
```

#### Roteiro do progresso

1. Gere os exemplos: `./scripts/exemplos/gerar-sessoes-exemplo.ps1`. Ele cria a raiz `datapilot-exemplo-fontes` (com changes ativas, uma arquivada e uma junção) e o arquivo `datapilot-exemplo-progresso.json`.
2. Abra o painel:

```bash
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-exemplo-progresso.json" --raiz-fontes "$TEMP/datapilot-exemplo-fontes"
```

3. **Ajuste NF-e**: o card **Faturamento** mostra `ajuste-nfe` e "4 de 6"; **Estoque**, `ajuste-nfe` e "3 de 6". O topo do trabalho não mostra soma.
4. **Relatorio mensal**: **Faturamento** mostra "2 de 2 · arquivada", **Compras** "progresso indisponível" (a pasta da change é uma junção), **Portal** "sem tarefas" e **Estoque** nenhuma change.
5. **Change que nao existe**: o card **Faturamento** mostra "change não encontrada", sem barra.
6. Com "Ajuste NF-e" selecionado, troque um `- [ ]` por `- [x]` em `datapilot-exemplo-fontes\Faturamento\openspec\changes\ajuste-nfe\tasks.md`: em até 5 segundos o card passa a "5 de 6", sem mudar o trabalho selecionado.

### Arquivo de dados

O painel guarda trabalhos, repositórios, sessões abertas por ele e as sessões excluídas em `%APPDATA%\Painel\demandas.json`. O arquivo só é criado na primeira alteração. Cada repositório pode ter `openspecChange`; a lista `excluidas` guarda, para cada sessão excluída, `agente`, `sessionId` e `excluidaEm`. O arquivo gravado pelas versões anteriores é lido sem conversão: a change gravada na demanda continua valendo para os repositórios que não têm a própria (e nunca é gravada de novo), e o arquivo sem `excluidas` é carregado normalmente. Recomenda-se copiar o arquivo antes do primeiro uso da versão nova. Se existir e não puder ser lido, o painel mostra um aviso com o caminho, não grava nada e desabilita criar e alterar trabalhos, abrir e retomar (a lista continua funcionando). Para usar outro arquivo, sem tocar nos dados reais:

```bash
dotnet run --project src/DataPilot.Wpf -- --dados "$TEMP/meus-dados.json"
```

Ao carregar, entradas inválidas são desconsideradas: trabalho com `id` ou título fora das regras ou com `id` repetido (vale o primeiro), repositório com pasta inválida ou repetida no mesmo trabalho, sessão com identificador que não seja GUID ou agente desconhecido, identificador de sessão repetido no arquivo (vale a primeira) e item de `excluidas` com agente desconhecido ou identificador fora da regra do hook. Um `openspecChange` inválido é tratado como ausente.

### Roteiro de verificação

Com os arquivos gerados pelo script (`datapilot-exemplo` e `datapilot-exemplo-varias-demandas.json`):

```bash
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-exemplo-varias-demandas.json"
```

- A barra superior mostra o alerta `2 precisam de atenção` e, ao lado, `4 aguardando você` e `4 executando` (as sessões registradas e as não registradas contam juntas; "sem status" não conta). A lista mostra, nesta ordem: "Ajuste NF-e" (precisa de atenção), "Relatorio mensal" (aguardando você), "Sem repositorios ainda" (nenhum repositório, nenhuma sessão) e **Sessões**. "Concluida" só aparece com **Mostrar arquivados**, por último e marcada como arquivada.
- Em "Ajuste NF-e": dois cards, `Faturamento` e `Estoque`; em `Estoque`, a sessão "ajuste de saldo" (executando) vem antes da "sem arquivo de status" (indicador cinza, com **Retomar**).
- `Faturamento` aparece em "Ajuste NF-e", em "Relatorio mensal" e em **Sessões**, cada um com a própria sessão.
- Em **Sessões**, as sessões registradas de `Faturamento` (encerrada) e `Estoque` têm **Retomar**, e a linha mostra o repositório de abertura (vale o registrado).
- Em "Sem repositorios ainda", a área orienta a vincular um repositório e não há cards.
- Para ver o aviso de dados indisponíveis, aponte `--dados` para um arquivo com texto que não seja JSON: **Novo trabalho** e as ações dos trabalhos ficam desabilitados e o arquivo não é alterado.
- Para ver o estado de nenhum trabalho, aponte `--dados` para um arquivo que não existe: a janela orienta a criar um trabalho ou usar **Sessões**.

Verificação com a conta `ia` das etapas anteriores (grupo 6 de `openspec/changes/etapa-4a-demandas/tasks.md`):

1. Copie o `demandas.json` real, abra o painel novo com `--dados` apontando para a cópia e confira que **Sessões** traz os repositórios e as sessões de antes, e que **Retomar** abre a conversa certa.
2. Confirme que o `ia` não lê o arquivo de dados (deve ser negado), executando como usuário principal: `ssh ia@localhost powershell -NoProfile -Command Get-Content $env:APPDATA\Painel\demandas.json`.

### Roteiro de trabalhos e sessões

O script também gera `datapilot-exemplo-trabalhos.json`, no formato novo: trabalhos com a change em cada repositório, um trabalho com a change herdada do campo antigo da demanda ("Trabalho antigo"), as sessões de **Sessões** (a sessão `11111111-...`, registrada em `Estoque`, e as lidas da pasta de status) e **uma sessão excluída** (`22222222-3333-4444-5555-666666666666`, do Claude em `Estoque`), que existe na pasta de status mas não aparece em lugar nenhum. Trabalhe numa cópia, porque criar, excluir e vincular gravam no arquivo:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
cp "$TEMP/datapilot-exemplo-trabalhos.json" "$TEMP/datapilot-trabalhos.json"
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-trabalhos.json" --raiz-fontes "$TEMP/datapilot-exemplo-fontes"
```

Confira na tela:

- **Sem identificador:** nenhum texto da janela nem dos diálogos mostra o identificador de um trabalho. **Novo trabalho** (**Ctrl+N**) abre um diálogo só com o título, sem texto de ajuda; um título vazio mostra o erro junto do campo.
- **Sessões:** a quantidade confere e a sessão excluída não aparece (nem no alerta). Cada linha mostra o indicador, o agente, o repositório, o tempo, **Retomar**, a lixeira e o menu com **Vincular a um trabalho**.
- **Linha de sessão num trabalho:** sem rótulo o lugar fica em branco, só **Retomar** e a lixeira, sem menu. O nome do status aparece na dica e, com o alto contraste ligado, em texto.
- **Changes por repositório:** "Ajuste NF-e" mostra `ajuste-nfe` e uma barra em cada card, sem soma no topo; "Trabalho antigo" mostra `ajuste-nfe` em `Estoque` (herdada do campo antigo da demanda).
- **Trocar change** (menu do repositório): lista as changes do repositório e **Nenhuma**.
- **Excluir:** a lixeira abre a confirmação "Excluir esta sessão?" com os botões **Excluir sessão** e **Cancelar**; **Enter** cancela.

Conferência manual com a conta `ia` e sessões reais (grupo 5 de `openspec/changes/trabalhos-e-sessoes/tasks.md`):

1. Crie um trabalho só com o título, vincule um repositório de `C:\Fontes` pela escolha de pasta e escolha uma change: o progresso aparece no card e nenhum identificador aparece.
2. Tente vincular uma pasta fora de `C:\Fontes` (por exemplo `C:\Windows`): o painel mostra o erro e nada é vinculado.
3. Em **Sessões**, use **Nova sessão** para abrir uma sessão do Claude e uma do Codex: as duas aparecem lá e **Retomar** abre a conversa certa. Abra uma sessão à mão, pelo SSH, e retome-a por **Sessões** escolhendo o repositório.
4. Exclua uma sessão de um trabalho e uma de **Sessões**: elas somem do painel, inclusive depois de reabri-lo, e **Enter** na confirmação não exclui.
5. Abra uma sessão por um trabalho cujo título contém `;`: o título da aba usa `,` no lugar.

### Roteiro do design e da usabilidade

Abra o painel com os exemplos (o arquivo de dados é uma cópia, porque ações como excluir e arquivar gravam nele):

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exemplos/gerar-sessoes-exemplo.ps1
cp "$TEMP/datapilot-exemplo-aguardando.json" "$TEMP/datapilot-design.json"
dotnet run --project src/DataPilot.Wpf -- --pasta-status "$TEMP/datapilot-exemplo" --dados "$TEMP/datapilot-design.json" --raiz-fontes "$TEMP/datapilot-exemplo-fontes"
```

- **Sem seleção:** a área da direita mostra só a orientação, sem botões, barra de progresso nem texto de trabalho.
- **Alerta:** o alerta vermelho mostra `2 precisam de atenção`.
- **Filtro:** **Ctrl+F** leva ao campo; `estoque` mostra "Ajuste NF-e" e **Sessões**; `relatorio` acha "Relatorio mensal" e a área continua em "Ajuste NF-e"; **Esc** limpa o filtro.
- **Faixas:** só as sessões em atenção (faixa vermelha) e aguardando (faixa amarela) são destacadas; a linha `aguardando a primeira mensagem` mostra **Abrir** e o menu com **Cancelar**.
- **Menus:** com o foco no cabeçalho de um repositório, **Shift+F10** abre "Mais ações" (Trocar change, Remover repositório).
- **Diálogos:** **Ctrl+N** abre o novo trabalho; **Esc** cancela.
- **Confirmação:** a lixeira de uma sessão abre a confirmação com os botões "Excluir sessão" e "Cancelar"; **Enter** cancela.
- **Faixa de erro:** com `--dados` apontando para um arquivo somente leitura, arquivar um trabalho mostra a faixa de erro no topo da área, com o botão de fechar, e o resto da janela segue utilizável.

Conferência manual (grupo 6 de `openspec/changes/archive/2026-11-18-design-e-usabilidade/tasks.md`):

1. Use o painel com sessões reais por um dia: as sessões que precisam de você são a primeira coisa que se vê.
2. Percorra a janela e todos os diálogos só com o teclado: o foco é sempre visível e os atalhos e menus funcionam.
3. Ligue o alto contraste do Windows com o painel aberto: a janela e os diálogos passam a usar as cores do sistema e os status continuam legíveis, agora com o nome em texto na linha.
