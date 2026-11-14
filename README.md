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

Para ver o painel sem a conta `ia`, gere os dados de exemplo (datas relativas ao momento da geração). O script cria a pasta de status e dois arquivos de dados, tudo em `$TEMP`:

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

## Demandas, abrir e retomar sessões (Etapas 3 e 4a)

A janela tem duas áreas. À esquerda, a lista de demandas; cada uma mostra a quantidade de repositórios e de sessões e o status mais urgente entre as sessões. A lista é ordenada pelo status mais urgente e, em empate, pela atualização mais recente; demandas sem sessões com status vêm depois, e as arquivadas por último. No fim da lista fica a entrada **Não vinculadas**. À direita, a demanda selecionada: `id`, título, change do OpenSpec (quando houver) e um card por repositório, com as sessões registradas nele.

A demanda e a rolagem escolhidas não mudam sozinhas quando um status muda ou a ordem das demandas muda.

### Ações

- **Nova demanda** (lista): `id` (1 a 30 caracteres de `[A-Za-z0-9_.-]`, nunca só pontos, único sem diferenciar maiúsculas), título (1 a 80 caracteres) e, opcionalmente, a change do OpenSpec (mesma regra de caracteres do `id`). O `id` `sem-demanda` é reservado.
- **Editar**, **Arquivar** e **Desarquivar** (topo da demanda). Arquivar uma demanda com sessão em andamento pede confirmação. **Mostrar arquivadas** (cabeçalho) exibe as arquivadas; elas não oferecem "Nova sessão" nem "Vincular repositório", mas "Retomar" continua disponível.
- A demanda **Sem demanda** guarda os repositórios e as sessões criados antes das demandas: aparece enquanto tiver repositórios e não pode ser editada nem arquivada.
- **Vincular repositório** (topo da demanda): nome da pasta dentro de `C:\Fontes` (só `[A-Za-z0-9_.-]`, nunca só pontos). As pastas já usadas em outras demandas aparecem como sugestão. O mesmo repositório pode estar em várias demandas, cada uma com as próprias sessões. **Remover** (no card) pede confirmação quando há sessões registradas na demanda e não afeta o mesmo repositório em outras demandas.
- **Nova sessão** (no card): escolhe o agente (Claude ou Codex) e um rótulo opcional (até 40 caracteres, sem `;`) e abre uma aba do Windows Terminal que chama o `abrir.ps1` como o usuário `ia`, pelo SSH local. Todas as abas ficam na janela `painel`. O título da aba é `id · repositório [· rótulo]`; para sessões de **Sem demanda**, o nome do agente ocupa o lugar do `id`. A sessão nova do Claude fica registrada no repositório dessa demanda.
- **Retomar**, **Mover** e **Remover** (na linha da sessão registrada). Retomar pede confirmação se a sessão não estiver encerrada, inativa nem sem status. **Mover** leva a sessão para outra demanda, no repositório de mesma pasta (vinculado ao destino se faltar). **Remover** só apaga o registro no painel, depois de confirmar: a conversa e a pasta de status não são tocadas.

Sessões registradas aparecem sempre na sua demanda, sem esconder as antigas; as que não têm arquivo de status aparecem como **sem status** (cinza, por último, e fora do resumo do cabeçalho). As sessões da pasta de status que o painel não registrou (as do Codex e a sessão nova que o Claude cria depois de um `/clear`) ficam em **Não vinculadas**, só para consulta e sem ações; a opção **Mostrar antigas** vale só para elas (encerradas ou inativas há mais de 3 dias ficam ocultas por padrão). O vínculo dessas sessões fica para a Etapa 4b.

O comando é montado só com dados do arquivo de dados ou da escolha na tela, nunca com valores lidos de arquivos de status, e iniciado como lista de argumentos, sem shell. Requer o Windows Terminal (`wt.exe`) e o SSH local da Etapa 1.

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
- Em **Não vinculadas**, as linhas não têm botões.
- Para ver o aviso de dados indisponíveis, aponte `--dados` para um arquivo com texto que não seja JSON: **Nova demanda** e as ações das demandas ficam desabilitadas e o arquivo não é alterado.
- Para ver o estado de nenhuma demanda, aponte `--dados` para um arquivo que não existe: a janela orienta a criar uma demanda e só mostra **Não vinculadas**.

O primeiro arquivo de exemplo (`datapilot-exemplo-demandas.json`, no formato da Etapa 3, só com **Sem demanda**) serve para conferir que o arquivo antigo continua sendo lido.

Verificação manual com a conta `ia` (grupo 6 de `openspec/changes/etapa-4a-demandas/tasks.md`):

1. Copie o `demandas.json` real, abra o painel novo com `--dados` apontando para a cópia e confira que **Sem demanda** traz os repositórios e as sessões de antes, e que **Retomar** abre a conversa certa.
2. Crie uma demanda, vincule dois repositórios que existam em `C:\Fontes` e abra uma sessão do Claude em cada um: elas aparecem agrupadas, o título de cada aba começa pelo `id` da demanda e o resumo na lista acompanha o status.
3. Mova uma sessão de **Sem demanda** para a demanda criada e retome-a: a conversa certa abre.
4. Abra uma sessão do Codex pelo card e execute `/clear` numa sessão do Claude: as duas sessões novas aparecem em **Não vinculadas**, sem ações.
5. Com uma demanda selecionada e sessões em andamento, use o painel por alguns minutos: a seleção e a rolagem não mudam sozinhas. Depois arquive e desarquive a demanda.
6. Confirme que o `ia` não lê o arquivo de dados (deve ser negado), executando como usuário principal: `ssh ia@localhost powershell -NoProfile -Command Get-Content $env:APPDATA\Painel\demandas.json`.
