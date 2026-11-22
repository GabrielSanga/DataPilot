# Tasks

Regra de todo grupo: o grupo só termina com `dotnet build` sem avisos novos e `dotnet test` verde. Movimentos de arquivo puros vão num commit separado das mudanças de código.

## 1. Models sem dependências

- [x] 1.1 Dividir `Models/Modelo.cs` em `Sessao.cs` (Sessao e enums) e `Constantes.cs`, e `Models/DadosModelo.cs` em `Demanda.cs` (records do arquivo de dados) e `RegrasDemanda.cs`, sem mudar código; verificar com `dotnet test`
- [x] 1.2 Mover `IdDemandaImplicita`, `TituloDemandaImplicita` e `ValidadeLancamento` de `DadosPainel` para `RegrasDemanda` e atualizar os chamadores; verificar que `grep -r "using DataPilot.Core.Persistence" src/DataPilot.Core/Models` não encontra nada e que `dotnet test` passa
- [x] 1.3 Criar `Models/Agentes.cs` (`NoArquivo`, `TentarLer`, `Nome`) e trocar `ValidadorEntrada.NomeAgente/TentarAgente`, o `switch` de agente de `ValidadorStatus` e as quatro expressões `== Claude ? "Claude" : "Codex"`; verificar que `grep -rn '"Claude" : "Codex"' src` não encontra nada e que `ValidadorStatusTests` e `ValidadorEntradaTests` passam
- [x] 1.4 Criar `RegrasDemanda.LancamentoValido` e usá-la em `VinculoAutomatico.Calcular`, `VisaoAgrupada.MontarLancamentos` e `AberturaSessao.ReabrirLancamento`; verificar que `grep -rn "ValidadeLancamento" src` só aparece em `RegrasDemanda` e na limpeza de vencidos de `DadosPainel`, e que os testes de lançamento passam
- [x] 1.5 Criar `ChaveSessao.De(agente, id)` e usá-la nos conjuntos de excluídas e registradas de `VisaoAgrupada` e `VinculoAutomatico`; adicionar um teste em `VinculoAutomaticoCorrespondenciaTests` com o mesmo id em outro agente (resultado: nenhum vínculo gravado); verificar com `dotnet test`

## 2. Leitura segura de arquivos em um lugar

- [x] 2.1 Dividir `OpenSpec/ProgressoOpenspec.cs` em `ContadorTarefas.cs`, `LocalizadorTasks.cs` e `LeitorTasks.cs`, e `OpenSpec/RepositoriosNaRaiz.cs` em `EscolhaDePasta.cs` e `ChangesDoRepositorio.cs`, sem mudar código; verificar com `dotnet test`
- [x] 2.2 Criar `Core/Arquivos/LeituraSegura.cs` com `Tipo` (movido de `LocalizadorTasks`), `LerTexto(caminho, limite)` (limite, BOM, UTF-8 estrito, abrindo sem bloquear) e `Subpastas(pasta)` (sem atalhos); comparar antes as cópias de `LeitorPasta` e `LeitorTasks` linha a linha e manter a conferência do arquivo aberto no `LeitorTasks`; verificar que `LeitorPastaTests` e `LeitorTasksTests` passam sem alteração
- [x] 2.3 Usar `LeituraSegura` em `LeitorPasta`, `LeitorTasks`, `LocalizadorTasks` e `ChangesDoRepositorio`; verificar que `grep -rn "Utf8Estrito\|0xEF" src` só aparece em `LeituraSegura` e que `dotnet test` passa
- [x] 2.4 Mover `MainViewModel.PastasDaRaiz` para `EscolhaDePasta.PastasDaRaiz(raiz)` usando `LeituraSegura.Subpastas`, com teste em `EscolhaDePastaTests` (ignora atalho, nome inválido e arquivo; ordem alfabética); verificar com `dotnet test`
- [x] 2.5 Trocar `ProgressoDosRepositorios.Montar` por `LeitorTasks.LerRepositorios`, mover o teste dele e apagar a classe; verificar com `dotnet test`

## 3. Regras de tela e de vínculo

- [x] 3.1 Dividir `Display/NavegacaoDemandas.cs` em `FiltroDemandas.cs` e `ProximaSessao.cs`, mover `ResumoCabecalho` para o próprio arquivo e renomear `Linking/VinculoSessao.cs` para `PedidoVinculo.cs`, sem mudar código; verificar com `dotnet test`
- [x] 3.2 Criar `RegrasExibicao.ComparaSessoes(status, data, id)` e usá-la em `RegrasExibicao.Montar`, `VisaoAgrupada.ComparaSessoes` (apagado) e `DemandaItem.AtualizarSessoes`; verificar que `VisaoAgrupadaTests`, `SessoesNaVisaoTests` e `RegrasExibicaoTests` passam
- [x] 3.3 Criar `RegrasExibicao.Quantidade` e usá-la em `VisaoAgrupada.TextoResumo` e no resumo de "Sessões" em `DemandaItem`; verificar que os testes de resumo passam
- [x] 3.4 Mover `Correspondencia.ExigeConfirmacao` para `RegrasExibicao`, juntar `CorrespondenciaTests` em `RegrasExibicaoTests` e apagar `Correspondencia.cs`; verificar com `dotnet test`
- [x] 3.5 Fazer `RegrasVinculo.PastaCoincide/AvisarPastaDivergente` receberem a pasta de trabalho (texto), adicionar `PedidoVinculo.Criar(SessaoRegistrada)` e apagar `PodeVincularSessao` e `PodeVincular` (a tela usa `Criar(...) is null` e `MotivoIdentificadorInvalido`); ajustar `VinculoSessaoTests`; verificar com `dotnet test`

## 4. Arquivo de dados e validação

- [x] 4.1 Dividir `DadosPainel.cs` em arquivos `partial` por seção (`DadosPainel.cs` com leitura e gravação, `.Demandas.cs`, `.Repositorios.cs`, `.Sessoes.cs`, `.Lancamentos.cs`), sem mudar código; verificar com `dotnet test`
- [x] 4.2 Adicionar `MensagemPasta`, `MensagemRotulo`, `MensagemTitulo` e `MensagemChange` em `ValidadorEntrada` e usá-las em `DadosPainel`, `AberturaSessao`, `MontadorComando` e `EscolhaDePasta`, mantendo o texto mais completo; verificar que `grep -rn "use até 40 caracteres\|use apenas letras" src` só aparece em `ValidadorEntrada` e que `dotnet test` passa (ajustando só asserções de texto que mudaram)
- [x] 4.3 Apagar `ValidadorEntrada.IdTrabalhoGeradoValido` (não é usado); verificar que `dotnet build` passa
- [x] 4.4 Apagar os apelidos `DadosPainel.RemoverSessao` e `CancelarLancamento`, trocar os chamadores e os testes por `DesfazerRegistro` e `DesfazerLancamento`; verificar com `dotnet test`
- [x] 4.5 Extrair `EstaArquivada(no)` (três cópias) e `RetirarSessoes(raiz, id)` (laço de `ExcluirSessao` e `DesfazerRegistro`); verificar que `DadosPainelTests` e `ExcluirSessaoTests` passam

## 5. Abertura de sessões

- [x] 5.1 Extrair `AberturaSessao.Executar(montar, gravar, desfazer)` e reescrever `Nova`, `ReabrirLancamento`, `RetomarNaoRegistrada` e `Retomar` com ele, mantendo a ordem montar, gravar, disparar e desfazer; verificar que todos os testes de `tests/.../Sessions` passam sem alteração

## 6. WPF

- [x] 6.1 Ajustar `MainViewModel` às mudanças do Core: `EscolhaDePasta.PastasDaRaiz`, `LeitorTasks.LerRepositorios`, `RegrasExibicao.ExigeConfirmacao`, `PedidoVinculo.Criar(SessaoRegistrada)` no lugar da `Sessao` artificial, sem revalidação no diálogo de vínculo e com `LancamentoExiste(id)` único em `AbrirLancamento/CancelarLancamento`; verificar que `dotnet build` passa e que `grep -n "new Sessao(" src/DataPilot.Wpf` não encontra nada
- [x] 6.2 Usar `Agentes.Nome` em `SessaoItem` e `LancamentoItem`; verificar com `dotnet build`
- [x] 6.3 Passar `EscolhaPastaWindows` para dentro de `InteracaoJanelas.EscolherPasta` e os records de `Wpf/Models` para `IInteracao.cs`, apagando os arquivos e a pasta; verificar com `dotnet build`
- [ ] 6.4 Teste manual do app (`dotnet run --project src/DataPilot.Wpf` com `--dados`, `--pasta-status` e `--raiz-fontes` apontando para cópias de teste): criar trabalho, vincular repositório e trocar a change, abrir sessão Claude e Codex, retomar, vincular uma sessão de "Sessões", excluir sessão, F8 e filtro; verificar que tudo se comporta como no `main`

## 7. Itens da lista sem callback

- [x] 7.1 Trocar os `Action<T>` dos construtores de `SessaoItem`, `LancamentoItem`, `CardItem` e `DemandaItem` por uma referência ao `MainViewModel` (`RetomarCommand = new RelayCommand(() => _painel.Retomar(this))`), tornando `internal` os métodos de ação chamados pelos itens; verificar com `dotnet build` e que `grep -rn "Action<" src/DataPilot.Wpf/ViewModels` não encontra nada
- [x] 7.2 Remover as fábricas `Func<string, …>` de `DemandaItem.Atualizar`, `DemandaItem.AtualizarSessoes` e `CardItem.Atualizar`, criando os filhos com `new …(chave, _painel)`, e apagar `NovoCard`, `NovaSessaoItem` e `NovoLancamentoItem` do `MainViewModel`; verificar com `dotnet build` e que `grep -rn "Func<" src/DataPilot.Wpf/ViewModels` não encontra nada
- [x] 7.3 Trocar `FiltroDemandas.Filtrar<T>` por chamadas diretas a `FiltroDemandas.Casa` em `MainViewModel.CasaComFiltro` (item sem demanda, a entrada "Sessões", continua sempre visível), apagar `Filtrar` e reescrever `FiltroDemandasTests` sobre `Casa` com os mesmos cenários; verificar com `dotnet test`
- [ ] 7.4 Teste manual dos comandos dos itens, inclusive pelos `ContextMenu` compartilhados (botão "…" e botão direito): Editar, Arquivar e Desarquivar trabalho; Vincular repositório, Trocar change e Remover repositório; Nova sessão no card e em "Sessões"; Retomar, Excluir e Vincular a um trabalho; Abrir e Cancelar lançamento; filtro de trabalhos com "Sessões" sempre visível; verificar que cada ação abre o diálogo ou faz a operação esperada

## 8. Documentação e fechamento

- [x] 8.1 Escrever `docs/arquitetura.md` (uma página): mapa das pastas do Core e do WPF com uma linha cada, o fluxo de um ciclo (monitor → vínculo automático → visão → tela) e a correspondência "Demanda no código = Trabalho na tela"; linkar no `README.md` e em `docs/desenvolvimento.md`; verificar que os caminhos citados existem
- [x] 8.2 Atualizar a linha de `ViewModels` em `docs/arquitetura.md`: os `*Item` chamam as ações do `MainViewModel` diretamente, e os callbacks que ficam são os diálogos que gravam (`aplicar`) e a infraestrutura (eventos, `DadosPainel.Alterar`, `AberturaSessao.Executar`, `SincronizacaoColecao`); verificar que os nomes citados existem no código
- [x] 8.3 Verificação final: `dotnet build` sem avisos novos, `dotnet test` verde, nenhum cenário de comportamento removido (comparar a lista de testes com o `main`, sendo aceitas só remoções de testes de código apagado) e `openspec validate refatorar-estrutura` passando

## Workflow follow-up

- Abrir PR de `refatorar-estrutura` para `main`.
- Arquivar o change depois do merge.
