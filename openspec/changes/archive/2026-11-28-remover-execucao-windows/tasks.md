# Tasks

Tarefas marcadas com **[manual]** são executadas pelo usuário, porque exigem o Docker Desktop ou a área de trabalho do Windows. Ao fim de cada grupo de código, `dotnet build` sem avisos e `dotnet test` passando.

## 1. Configurações e registros no Core

- [x] 1.1 Reduzir `Configuracoes` a `record Configuracoes(string Container)` e remover o enum `Execucao` e `Execucoes` (decisões 1 e 2). Remover `UsuarioSshPadrao` e `HostSsh` de `Constantes`, e `UsuarioSshValido`/`MensagemUsuarioSsh` de `ValidadorEntrada`, junto com os comentários que citam o `abrir.ps1`. Fazer o `SalvarConfiguracoes` gravar só `container` e o `ExtrairConfiguracoes` ler só `container`. Em `ConfiguracoesTests`, cobrir os cenários de "Valores padrão" e "Configurações gravadas no arquivo de dados" (campos antigos desconsiderados e removidos ao salvar). Apagar `UsuarioSshTests` e verificar que os testes passam.
- [x] 1.2 Remover `Execucao` de `SessaoRegistrada` e `LancamentoPendente` e o parâmetro `execucao` de `RegistrarSessao`, `RegistrarLancamento` e `RegistrarRetomada`. Gravar sempre `"execucao": "container"` e trocar o `TentarLerExecucao` pela exigência desse valor. Atualizar o `VinculoAutomatico`, que deixa de copiar o ambiente. Reescrever `ExecucaoNosRegistrosTests` com os cenários de "Ambiente de execução nos registros": sem campo, `windows` e `wsl` desconsiderados, registro desconsiderado preservado numa gravação do mesmo repositório, vínculo gravando `container`. Verificar que passam.

## 2. Abertura só no container

- [x] 2.1 Reduzir o `MontadorComando` ao caminho do container e tornar `ComandoTerminal.Container` obrigatório (decisão 3). Fazer o `DisparadorContainer` verificar sempre e tirar das mensagens dele e de `LocaisContainer.MensagemFora` a sugestão de "Windows". Na `AberturaSessao`, deixar de repassar o ambiente, e em `NovaEmSessoes` tornar a recusa de pasta fora dos locais incondicional. Remover `PastaScripts` e `ScriptAbrir` de `Constantes`.
- [x] 2.2 Ajustar os testes da abertura:
  - apagar `MontadorComandoSshTests` e tirar `Fabrica.Windows`/`Fabrica.Ssh`;
  - em `MontadorComandoTests`, `MontadorComandoCaminhoTests`, `MontadorComandoDemandaTests` e `TituloDaAbaTests`, passar a esperar o comando do container;
  - em `ContainerTests`, tirar a retomada no Windows e a mensagem com "Windows";
  - ajustar `AberturaSessaoTests`, `AberturaSessaoDemandaTests`, `NovaSessaoEmSessoesTests` e `ReabrirLancamentoTests`.

  Cobrir os cenários de "Composição do comando no container" (inclusive a retomada), "Locais disponíveis no container" (repositório antigo fora dos locais) e "Ambiente do container indisponível" (mensagem sem outro ambiente). Verificar que passam.

## 3. Status e vínculo

- [x] 3.1 Fazer o `ValidadorStatus` exigir `cwd` como texto que começa com `/` (decisão 4). Em `ValidadorStatusTests`, cobrir os cenários "Pasta de trabalho do Windows" e "Campo obrigatório ausente" sem `cwd`, e passar as fixtures válidas para `/workspace/...`. Verificar que passam.
- [x] 3.2 Fazer o `LeitorPasta` deixar a data de início nula quando não houver marcador `.criado` (decisão 5). Em `LeitorPastaTests`, cobrir "Arquivo de status sem marcador" (listada e sem data) e "Marcador que é atalho". Verificar que passam e que o `VinculoAutomatico` ignora a sessão sem data.
- [x] 3.3 Tirar do `PedidoVinculo.PastaCoincide` a comparação direta com o caminho do Windows (decisão 6). Passar para `/workspace/...` as fixtures com `cwd` do Windows em `Fabrica`, `ApoioVinculoAutomatico`, `SessoesNaVisaoTests`, `TextoExibicaoTests`, `VinculoAutomaticoAplicacaoTests`, `VinculoAutomaticoCorrespondenciaTests`, `VinculoPorLancamentoTests`, `VinculoSessaoTests` e `DadosPainelTests`. Mantenha `C:\...` onde o valor é a pasta do repositório, e não o `cwd`. Verificar que todos os testes passam.
- [x] 3.4 Fazer o `scripts/exemplos/gerar-sessoes-exemplo.ps1` gravar `cwd` em `/workspace/<pasta>` e criar o marcador `<id>.criado` de cada sessão. Rodar o script e abrir o painel com `--pasta-status` e `--raiz-fontes` apontando para os exemplos (como em `docs/desenvolvimento.md`). Conferir que as sessões aparecem nos cards e em "Sessões".

## 4. Vincular repositório

- [x] 4.1 Fazer o `EscolhaDePasta.Validar` recusar com `MensagemFora` um caminho completo fora de `C:\Fontes` e `C:\openspec\ERP`, mantendo a pasta direta da raiz pelo nome e a recusa de atalho e junção (decisão 7). Trocar o comentário que cita o `abrir.ps1`. Em `EscolhaDePastaTests`, cobrir os cenários de "Vincular repositório pela pasta": subpasta da raiz dos fontes, pasta do OpenSpec, pasta fora da raiz, junção. Verificar que passam.

## 5. Scripts do Windows e tela

- [x] 5.1 Remover `ScriptsDoPainel.cs`, `ScriptsDoPainelTests.cs`, os dois `EmbeddedResource` do `DataPilot.Core.csproj`, a instalação e o `--pasta-scripts` do `App.xaml.cs` e o `AvisarScripts` do `MainViewModel`. Apagar `scripts/abrir.ps1`, `scripts/hook.ps1`, `scripts/testes/verificar.ps1`, `scripts/exemplos/claude-settings.json` e `scripts/exemplos/codex-hooks.json` (decisão 8). Verificar que `git grep -n -i -E "abrir\.ps1|hook\.ps1|pasta-scripts|C:\\\\Painel"` não acha nada em `src`, `tests` e `scripts`, e que build e testes passam.
- [x] 5.2 Deixar o `ConfiguracoesWindow` só com a pasta de status e o nome do container, sempre habilitado (decisão 9). **[manual]** Abrir o painel com um `demandas.json` de teste que tenha `execucao: windows`, `usarSsh` e `usuarioSsh` nas configurações. Conferir que o diálogo mostra só os dois campos, que um nome inválido mostra o erro junto do campo e que, ao salvar, a seção `configuracoes` fica só com `container`.
- [x] 5.3 Tirar dos comentários de `docker/painel/abrir.sh`, `docker/painel/hook.mjs` e `docker/testes/verificar.sh` as referências aos scripts do Windows. Rodar `verificar.sh` dentro do container e conferir que todos os casos continuam passando.

## 6. Documentação

- [x] 6.1 Apagar `docs/instalacao-etapa-1.md`. No `docs/instalacao-container.md`:
  - trocar "Voltar para o Windows" por "Remover a instalação antiga do modo Windows (opcional)" (OpenSSH Server, usuário `ia` e perfil, `C:\Painel`), com o aviso de não apagar `C:\ProgramData\Painel\sessoes`;
  - tirar do parágrafo da permissão da pasta de status a citação ao guia removido.

  Verificar que os comandos citados existem no PowerShell como escritos.
- [x] 6.2 Atualizar `docs/PRD.md` (o isolamento só pelo container), `docs/arquitetura.md` (sem `scripts/abrir.ps1`/`hook.ps1`, `ScriptsDoPainel` nem ambiente por sessão), `docs/desenvolvimento.md` (sem passos com `ia`, SSH ou `C:\Painel`) e `README.md` (requisitos só com Docker Desktop). Verificar com `git grep -n -i -E "ssh|usuário .ia.|instalacao-etapa-1|abrir\.ps1|hook\.ps1|modo Windows"` em `docs` e `README.md` que só restam menções na seção de remoção da instalação antiga.

## 7. Verificação manual

- [x] 7.1 **[manual]** Com o `demandas.json` real, abrir o painel e conferir:
  - as sessões antigas do Windows não aparecem nos cards;
  - as do container aparecem e retomam com histórico;
  - uma sessão nova do Claude e uma do Codex abrem no container, e o Codex é vinculado automaticamente;
  - o arquivo continua com os registros antigos (por exemplo, a contagem de `"sessionId"` não diminuiu).
- [x] 7.2 **[manual]** Com um arquivo de status antigo do Windows (`cwd` `C:\Fontes\...`) na pasta de status, ligar "Mostrar antigas" e conferir que ele não aparece. Num repositório vinculado fora de `C:\Fontes` e `C:\openspec\ERP`, acionar "Nova sessão" e conferir a mensagem com os dois locais, sem sugerir "Windows". Tentar vincular `D:\` e conferir a recusa.

## Workflow follow-up

- Ao arquivar, apagar `openspec/specs/scripts-do-painel/`, que fica sem requisitos.
- Ao arquivar, atualizar o Purpose de `abertura-de-sessao` (sem `ia` nem SSH local), `configuracoes` (pasta de status e nome do container) e `lancamento-de-sessao` (abas que rodam no container), editando direto em `openspec/specs/`.
- Ajustar a change `terminal-embutido` antes de implementá-la: sem sessões do Windows, sem "abridor por ambiente", e `EstadoTerminal` para todas as sessões.
