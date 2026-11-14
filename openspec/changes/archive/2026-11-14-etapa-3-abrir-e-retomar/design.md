# Design

## Context

Motivação e escopo: ver `proposal.md`. Requisitos: ver as três specs em `specs/`.

O que molda a solução:

- É a primeira etapa em que o painel executa algo. Ele roda como o usuário principal, com acesso aos servidores da empresa; os arquivos de status são escritos pelo usuário `ia` e são não confiáveis.
- O contrato de lançamento já existe e foi validado à mão na Etapa 1: `ssh -t ia@localhost powershell -NoProfile -ExecutionPolicy Bypass -File C:\Painel\abrir.ps1 -Agente … -Repo … [-Sessao …] -Modo …`. O `abrir.ps1` valida tudo de novo e não aceita valores com espaço nem aspas.
- Resultados da Etapa 1 que limitam esta etapa: o identificador do Codex só aparece pelo hook, e `/clear` no Claude cria uma sessão com outro identificador.
- O código da Etapa 2 concentra modelo, leitura e regras no `DataPilot.Core`; a janela usa um view model que recebe retratos da pasta de status.
- Decisões tomadas na exploração: vincular sessões fica para a Etapa 4; a lista de repositórios vem de um cadastro no painel.

## Goals / Non-Goals

**Goals:**

- Tornar impossível, por construção, que um valor de arquivo de status chegue a um argumento de comando.
- Montagem do comando como função pura, testada argumento a argumento.
- Arquivo de dados que a Etapa 4 possa estender sem migração.
- Nunca perder dados do usuário: arquivo ilegível não é sobrescrito, e a gravação é atômica.

**Non-Goals:**

- Vincular sessões que o painel não abriu.
- Telas de demandas; a demanda implícita não aparece na interface.
- Saber se a aba abriu com sucesso ou acompanhar o processo do agente. O painel dispara e o resultado aparece no status.
- Impedir duas instâncias do painel.

## Decisions

### 1. Dois tipos de dados que não se misturam

O Core passa a ter tipos distintos para o que vem do arquivo de dados (`RepositorioCadastrado`, `SessaoRegistrada`) e mantém `Sessao` para o que vem da pasta de status. O montador de comandos só aceita os tipos do arquivo de dados.

- Por quê: a regra "nada de arquivo de status em comando" vira um erro de compilação em vez de uma revisão manual. `Sessao.Cwd` simplesmente não tem como chegar ao montador.
- A correspondência entre os dois mundos é feita num único lugar: uma função pura que, dadas as sessões da lista e as registradas, diz quais linhas são conhecidas, comparando identificador e agente. O que sai dela para a ação "Retomar" é a `SessaoRegistrada`.

### 2. Montador de comando puro e disparo atrás de uma interface

`MontadorComando` devolve o executável (`wt.exe`) e a lista de argumentos. Um `IDisparador` recebe isso e inicia o processo com `ProcessStartInfo.ArgumentList` e `UseShellExecute = false`.

- Por quê: a lista de argumentos é o contrato com o `abrir.ps1` e precisa de testes exatos. Com o disparo atrás de uma interface, os testes usam um disparador falso e nenhum teste abre janela.
- `ia@localhost`, `C:\Painel\abrir.ps1` e o nome da janela (`painel`) ficam em `Constantes`.
- O montador valida de novo agente, pasta e identificador e falha se algo estiver fora do formato, mesmo que quem chamou já tenha validado.

### 3. Validação num único tipo

`ValidadorEntrada` concentra as três regras: pasta (`[A-Za-z0-9_.-]`, não só pontos), identificador (GUID canônico 8-4-4-4-12) e rótulo (até 40 caracteres, sem `;` e sem caracteres de controle). Cadastro, carga do arquivo e montador usam o mesmo código.

- Por quê: as regras da pasta e do GUID são as mesmas do `abrir.ps1`; mantê-las num só lugar evita que as duas pontas divirjam.
- O `;` é proibido no rótulo porque o Windows Terminal o trata como separador de comandos em qualquer argumento. Os demais argumentos já têm conjuntos de caracteres que não o incluem.

### 4. Arquivo de dados como árvore JSON preservada

`DadosPainel` lê o `demandas.json` como árvore (`JsonNode`), altera só os nós que conhece e grava a árvore inteira. Cada operação relê o arquivo, aplica a mudança e grava num temporário na mesma pasta, trocando-o sobre o destino.

- Por quê: preservar o que não se reconhece permite que a Etapa 4 acrescente campos e que o usuário edite o arquivo à mão sem perder dados. Reler antes de gravar reduz o dano de duas instâncias abertas.
- Arquivo inexistente é estado vazio. Arquivo que não é JSON, ou cuja raiz não tem a estrutura esperada, põe o painel em modo "dados indisponíveis": nada é gravado e as ações são desabilitadas.
- O caminho padrão é `%APPDATA%\Painel\demandas.json`, como no PRD. `--dados <arquivo>` substitui o caminho, para desenvolvimento e testes manuais sem tocar nos dados reais; o valor vem de quem inicia o painel.
- Alternativa descartada: classes tipadas com `JsonSerializer`. Mais simples de ler, mas descarta campos desconhecidos na gravação.

### 5. Ordem das operações em "Nova sessão" do Claude

Gerar o GUID, registrar a sessão no arquivo de dados, disparar o terminal; se o disparo falhar, remover o registro.

- Por quê: registrar antes garante que uma sessão aberta sempre possa ser retomada, mesmo que o painel feche logo depois. O inverso deixaria sessões órfãs, que nesta etapa não teriam como ser adotadas.
- O Codex não passa por esse caminho: nada é registrado.

### 6. Interface

O cabeçalho ganha "Nova sessão" e "Repositórios". "Nova sessão" abre um diálogo pequeno (repositório, agente, rótulo). "Repositórios" abre um diálogo com a lista, um campo para adicionar e a remoção. A linha da lista ganha o botão "Retomar" e a indicação do repositório cadastrado e do rótulo quando a sessão é conhecida.

- Por quê: a janela continua sendo a lista da Etapa 2; as ações novas são poucas e cabem em diálogos, sem antecipar o leiaute por demanda da Etapa 4.
- Confirmações (sessão possivelmente aberta, remoção com sessões) e erros usam caixas de mensagem simples.
- O view model recebe `DadosPainel` e `IDisparador` por construtor; a lógica de decisão (quais linhas são conhecidas, se pede confirmação, título da aba) fica em funções puras no Core.

### 7. Testes

Testes unitários para validação, montador (lista exata de argumentos de cada combinação de agente e modo, e recusa de valores inválidos), correspondência de sessões conhecidas, título da aba e `DadosPainel` sobre arquivos temporários (primeiro uso, cadastro, duplicado, remoção, registro e desfazer, entradas inválidas, arquivo corrompido não sobrescrito, campo desconhecido preservado).

- O disparo real do `wt.exe` é verificado uma vez, cedo, por um ensaio isolado, e depois no roteiro manual.

## Risks / Trade-offs

- **`wt.exe` é um atalho de execução do Windows e pode não iniciar sem shell, ou tratar o título de forma inesperada** → primeira tarefa da change: um ensaio que dispara `wt.exe` com `ArgumentList` e um comando inofensivo, antes de construir o resto. Se falhar, o design do disparo é revisto.
- **Codex sem retomada e Claude sem retomada depois de `/clear`** → limite aceito desta etapa; a interface não oferece o botão nesses casos, em vez de oferecer e falhar. Resolvido pelo vincular da Etapa 4.
- **Arquivo de status forjado com o identificador de uma sessão registrada** → o `ia` pode fazer o botão "Retomar" aparecer numa linha com dados de exibição enganosos, mas o comando usa só os dados registrados, e a linha indica o repositório cadastrado. O efeito máximo é o usuário retomar uma sessão que é dele.
- **Duas instâncias do painel gravando ao mesmo tempo** → reler antes de gravar reduz a janela; a última gravação vence. Aceito nesta etapa.
- **Painel não sabe se a aba abriu de fato** → erros do `abrir.ps1` (repositório não encontrado, por exemplo) aparecem na própria aba; se o hook não disparar, a sessão registrada não aparece na lista. Registrado como limite na proposta.
- **Chave SSH com senha e sem agente** → o pedido de senha aparece na aba. Fora do controle do painel; coberto por RS-7 do PRD.
- **Rótulo limitado** → 40 caracteres sem `;` é restritivo, mas evita qualquer tratamento de escape no título.

## Migration Plan

Nenhuma migração: o arquivo de dados não existia. Na primeira alteração, o painel cria `%APPDATA%\Painel\demandas.json`. Reverter é voltar à versão anterior do painel; o arquivo pode ficar, pois nada mais o lê.

## Open Questions

- Onde mostrar sessões registradas que não têm arquivo de status. Fica para a Etapa 4, que reorganiza a tela por demanda.
- Se o cadastro deve aceitar um nome de exibição diferente da pasta (`nome` no formato do PRD). Nesta etapa os dois são iguais.

## Resultado do ensaio do disparo (tarefa 1.1)

Executado em 2026-11-14 com Windows Terminal 1.25 e `ProcessStartInfo.ArgumentList` (`UseShellExecute = false`):

- `wt.exe` inicia sem shell e o processo retorna código 0; a aba abre na janela nomeada pelo `-w`, e uma segunda chamada com o mesmo nome entra na mesma janela.
- O título `Ensaio · DataPilot` (com o separador `·`) aparece corretamente na aba, e `--suppressApplicationTitle` impede que o PowerShell o troque.
- Confirmado que `;` dentro de qualquer argumento é tratado pelo `wt` como separador de comandos: um `-Command "a; b"` abriu abas extras e o título se perdeu. Isso justifica a proibição de `;` no rótulo.

A decisão 2 do design se mantém. O código do ensaio ficou fora da solution e foi removido.
