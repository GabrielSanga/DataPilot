using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core;

namespace DataPilot.Wpf;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly MonitorPasta _monitor;
    private readonly TimeProvider _relogio;
    private readonly Dispatcher _dispatcher;
    private readonly DadosPainel _dados;
    private readonly AberturaSessao _abertura;
    private readonly IInteracao _interacao;
    private readonly LeitorTasks _leitorTasks;
    private RetratoPasta? _retrato;
    private Visao? _visao;
    private EstadoDados _estadoDados = EstadoDados.Vazio;
    private readonly DemandaItem _sessoes;
    private readonly ICollectionView _vista;

    /// <summary>Todas as entradas da lista: trabalhos na ordem de urgência e, no fim, "Sessões".</summary>
    public ObservableCollection<DemandaItem> Demandas { get; } = [];

    /// <summary>A lista como a tela a mostra: <see cref="Demandas"/> depois do filtro.</summary>
    public ICollectionView DemandasFiltradas => _vista;

    [ObservableProperty] private DemandaItem? _demandaSelecionada;
    [ObservableProperty] private string _filtroTexto = "";
    [ObservableProperty] private bool _pastaIndisponivel;
    [ObservableProperty] private string _aviso = "";
    [ObservableProperty] private bool _mostrarAntigas;
    [ObservableProperty] private bool _mostrarArquivadas;
    [ObservableProperty] private bool _semDemandas;
    [ObservableProperty] private bool _dadosDisponiveis = true;
    [ObservableProperty] private string _avisoDados = "";

    // Alerta do topo (decisão 5 do design).
    [ObservableProperty] private NivelSinal _alertaNivel = NivelSinal.Neutro;
    [ObservableProperty] private string _alertaTexto = "Carregando…";
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(IrParaProximaCommand))] private bool _podeIrParaProxima;
    /// <summary>"4 aguardando você" ao lado do alerta vermelho; vazio quando não há ou quando o alerta já é esse status.</summary>
    [ObservableProperty] private string _contagemAtencao = "";
    [ObservableProperty] private string _contagemAguardando = "";
    [ObservableProperty] private string _contagemExecutando = "";

    // Faixa de erros e mensagens, dentro da janela.
    [ObservableProperty] private string _mensagem = "";
    [ObservableProperty] private bool _mensagemEhErro;
    [ObservableProperty] private bool _temMensagem;

    public MainViewModel(string pasta, TimeProvider relogio, Dispatcher dispatcher,
        DadosPainel dados, AberturaSessao abertura, IInteracao interacao, LeitorTasks leitorTasks)
    {
        _leitorTasks = leitorTasks;
        _relogio = relogio;
        _dispatcher = dispatcher;
        _dados = dados;
        _abertura = abertura;
        _interacao = interacao;
        _sessoes = NovoDemandaItem(DemandaItem.ChaveSessoes, true);
        _vista = CollectionViewSource.GetDefaultView(Demandas);
        _vista.Filter = CasaComFiltro;
        _monitor = new MonitorPasta(pasta);
        Aviso = $"Pasta de status indisponível: {TextoExibicao.Limpar(pasta)}";
        _monitor.RetratoAtualizado += r => _dispatcher.BeginInvoke(() => Receber(r));
        RecarregarDados();
    }

    public void Iniciar() => _monitor.Iniciar();

    // Ao trocar de trabalho o progresso aparece na hora, sem esperar o ciclo seguinte.
    partial void OnDemandaSelecionadaChanged(DemandaItem? value) => AtualizarProgresso();

    /// <summary>
    /// Relê o progresso da change de cada repositório só do trabalho selecionado (o único que o mostra), a cada
    /// atualização da tela e ao trocar de trabalho ou de change. Não há <c>FileSystemWatcher</c> nas pastas do <c>ia</c>.
    /// </summary>
    private void AtualizarProgresso()
    {
        if (DemandaSelecionada is not { Demanda: { } demanda } item) return;
        item.AplicarProgresso(ProgressoDosRepositorios.Montar(demanda.Repositorios, _leitorTasks));
    }

    partial void OnMostrarAntigasChanged(bool value) => Aplicar();
    partial void OnMostrarArquivadasChanged(bool value) => Aplicar();

    // ---------- filtro de trabalhos ----------

    private bool CasaComFiltro(object o)
        => o is not DemandaItem item || FiltroDemandas.Filtrar(new[] { item }, FiltroTexto, i => i.Demanda).Count == 1;

    partial void OnFiltroTextoChanged(string value) => AtualizarFiltro();

    /// <summary>
    /// Reaplica o filtro. Filtrar não muda o trabalho selecionado: o aviso de que ele mudou só serve para a lista
    /// destacá-lo de novo quando ele volta a aparecer.
    /// </summary>
    private void AtualizarFiltro()
    {
        _vista.Refresh();
        OnPropertyChanged(nameof(DemandaSelecionada));
    }

    // ---------- faixa de erros e mensagens ----------

    private void MostrarErro(string mensagem)
    {
        Mensagem = mensagem;
        MensagemEhErro = true;
        TemMensagem = true;
    }

    private void MostrarInformacao(string mensagem)
    {
        Mensagem = mensagem;
        MensagemEhErro = false;
        TemMensagem = true;
    }

    [RelayCommand]
    private void FecharMensagem()
    {
        TemMensagem = false;
        Mensagem = "";
    }

    // ---------- fábricas dos itens (ligam as ações da tela às operações) ----------

    private DemandaItem NovoDemandaItem(string chave, bool sessoes)
        => new(chave, sessoes, EditarDemanda, ArquivarDemanda, DesarquivarDemanda, VincularRepositorio, NovaSessaoEmSessoes);

    private CardItem NovoCard(string pasta) => new(pasta, NovaSessao, TrocarChange, RemoverRepositorio);

    private LancamentoItem NovoLancamentoItem(string id) => new(id, AbrirLancamento, CancelarLancamento);

    private SessaoItem NovaSessaoItem(string sessionId) => new(sessionId, Retomar, ExcluirSessao, VincularSessao);

    // ---------- dados do painel ----------

    private void RecarregarDados()
    {
        _estadoDados = _dados.Carregar();
        DadosDisponiveis = _estadoDados.Disponivel;
        AvisoDados = _estadoDados.Disponivel ? ""
            : $"Dados do painel indisponíveis: {TextoExibicao.Limpar(_dados.Caminho)} ({TextoExibicao.Limpar(_estadoDados.Erro ?? "")}). "
              + "O arquivo não foi alterado; criar e alterar trabalhos, abrir e retomar estão desabilitados.";
    }

    private void RecarregarEAplicar()
    {
        RecarregarDados();
        Aplicar();
    }

    private void SelecionarPorChave(string chave)
        => DemandaSelecionada = Demandas.FirstOrDefault(d => d.Chave == chave);

    private static string Chave(string demandaId) => demandaId.ToLowerInvariant();

    // ---------- ir para a próxima sessão que precisa do usuário ----------

    /// <summary>"Ir para a próxima" e F8: seleciona o próximo item da lista com sessão no status destacado no topo.</summary>
    [RelayCommand(CanExecute = nameof(PodeIrParaProxima))]
    private void IrParaProxima()
    {
        if (_visao is null) return;
        var destino = ProximaSessao.Escolher(_visao, DemandaSelecionada?.Demanda?.Id, DemandaSelecionada?.EhSessoes == true);
        if (destino is null) return;

        var chave = destino.EhSessoes ? DemandaItem.ChaveSessoes : Chave(destino.DemandaId!);
        var item = Demandas.FirstOrDefault(d => d.Chave == chave);
        if (item is null) return;
        // Se o filtro esconde o destino, ele é limpo: levar o usuário a algo que não aparece não ajuda.
        if (!CasaComFiltro(item)) FiltroTexto = "";
        DemandaSelecionada = item;
    }

    /// <summary>Clique em "DataPilot": volta ao início, sem nenhum item selecionado.</summary>
    [RelayCommand]
    private void IrParaInicio() => DemandaSelecionada = null;

    // ---------- trabalhos ----------

    public void NovaDemanda()
    {
        if (!DadosDisponiveis) return;
        string? criada = null;
        _interacao.DialogoTrabalho(null, titulo =>
        {
            var r = _dados.CriarDemanda(titulo, _relogio.GetUtcNow());
            if (!r.Ok) return r.Mensagem;
            criada = r.Id;
            return null;
        });
        if (criada is null) return;
        RecarregarEAplicar();
        SelecionarPorChave(Chave(criada));
    }

    private void EditarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda || !RegrasDemanda.PodeEditar(demanda)) return;
        var editou = _interacao.DialogoTrabalho(demanda.Titulo, titulo =>
        {
            var r = _dados.EditarDemanda(demanda.Id, titulo);
            return r.Ok ? null : r.Mensagem;
        });
        if (editou) RecarregarEAplicar();
    }

    private void ArquivarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda) return;
        if (!RegrasDemanda.PodeArquivar(demanda))
        {
            MostrarErro(RegrasDemanda.EhImplicita(demanda.Id)
                ? "\"Sessões\" não pode ser arquivada." : "O trabalho já está arquivado.");
            return;
        }
        if (item.TemSessaoEmAndamento
            && !_interacao.Confirmar("Arquivar o trabalho?", "Este trabalho tem sessões em andamento.", "Arquivar"))
            return;
        var r = _dados.ArquivarDemanda(demanda.Id);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível arquivar o trabalho.");
    }

    private void DesarquivarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda) return;
        var r = _dados.DesarquivarDemanda(demanda.Id);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível desarquivar o trabalho.");
    }

    // ---------- repositórios ----------

    private void VincularRepositorio(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda || !RegrasDemanda.PodeVincularRepositorio(demanda)) return;

        var vinculou = _interacao.DialogoRepositorio(_leitorTasks.Raiz, null, null, (pasta, change) =>
        {
            var r = _dados.VincularRepositorio(demanda.Id, pasta, change);
            return r.Ok ? null : r.Mensagem;
        });
        if (vinculou) RecarregarEAplicar();
    }

    private void TrocarChange(CardItem card)
    {
        if (!DadosDisponiveis) return;
        var demanda = _estadoDados.Demandas.FirstOrDefault(d => string.Equals(d.Id, card.DemandaId, StringComparison.OrdinalIgnoreCase));
        var atual = demanda?.Repositorios.FirstOrDefault(r => string.Equals(r.Pasta, card.Pasta, StringComparison.OrdinalIgnoreCase))?.OpenspecChange;
        var trocou = _interacao.DialogoRepositorio(_leitorTasks.Raiz, card.Pasta, atual, (pasta, change) =>
        {
            var r = _dados.DefinirChange(card.DemandaId, pasta, change);
            return r.Ok ? null : r.Mensagem;
        });
        if (trocou) RecarregarEAplicar();
    }

    private void RemoverRepositorio(CardItem card)
    {
        if (!DadosDisponiveis) return;
        if (card.Sessoes.Count > 0)
        {
            var n = card.Sessoes.Count;
            var msg = n == 1
                ? "A sessão registrada neste repositório deixará de poder ser retomada neste trabalho."
                : $"As {n} sessões registradas neste repositório deixarão de poder ser retomadas neste trabalho.";
            if (!_interacao.Confirmar($"Remover o repositório {card.Pasta}?", msg, "Remover repositório")) return;
        }
        var r = _dados.RemoverRepositorio(card.DemandaId, card.Pasta);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível remover o repositório.");
    }

    // ---------- abrir, retomar e excluir sessões ----------

    private void NovaSessao(CardItem card)
    {
        if (!DadosDisponiveis) return;
        var demanda = DemandaSelecionada?.Demanda;
        if (demanda is null || !RegrasDemanda.PodeAbrirNovaSessao(demanda)) return;

        var escolha = _interacao.PedirNovaSessao();
        if (escolha is null) return;

        var r = _abertura.Nova(card.DemandaId, card.Pasta, escolha.Agente, escolha.Rotulo);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível abrir a sessão.");
    }

    /// <summary>"Nova sessão" em "Sessões": pede a pasta do repositório (só direta da raiz) e depois o agente.</summary>
    private void NovaSessaoEmSessoes(DemandaItem item)
    {
        if (!DadosDisponiveis) return;
        var caminho = _interacao.EscolherPasta(_leitorTasks.Raiz);
        if (caminho is null) return;
        var pasta = EscolhaDePasta.Validar(_leitorTasks.Raiz, caminho);
        if (!pasta.Ok)
        {
            MostrarErro(pasta.Erro!);
            return;
        }

        var escolha = _interacao.PedirNovaSessao();
        if (escolha is null) return;

        var r = _abertura.NovaEmSessoes(pasta.Pasta!, escolha.Agente, escolha.Rotulo);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível abrir a sessão.");
    }

    private void Retomar(SessaoItem item)
    {
        if (!DadosDisponiveis) return;

        if (Correspondencia.ExigeConfirmacao(item.Status)
            && !_interacao.Confirmar("Abrir outra aba desta sessão?",
                $"Esta sessão aparece como \"{item.StatusTexto}\" e pode já estar aberta no terminal.", "Abrir outra aba"))
            return;

        if (item.Registrada is { } registrada)
        {
            var r = _abertura.Retomar(registrada);
            if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível retomar a sessão.");
            return;
        }
        if (item.Lida is { } lida) RetomarNaoRegistrada(lida);
    }

    /// <summary>
    /// Retoma uma sessão da pasta de status que o painel não registrou: pede o repositório entre as pastas diretas da
    /// raiz, pré-selecionando a de mesmo nome que a pasta de trabalho, registra a sessão em "Sessões" e abre a conversa.
    /// </summary>
    private void RetomarNaoRegistrada(Sessao lida)
    {
        var pedido = PedidoVinculo.Criar(lida);
        if (pedido is null)
        {
            MostrarErro(RegrasVinculo.PodeVincularSessao(lida).Motivo ?? "Esta sessão não pode ser retomada.");
            return;
        }

        var pastas = PastasDaRaiz();
        if (pastas.Count == 0)
        {
            MostrarErro($"Nenhuma pasta encontrada em {TextoExibicao.Limpar(_leitorTasks.Raiz)}.");
            return;
        }
        var sugerida = pastas.FirstOrDefault(p => RegrasVinculo.PastaCoincide(lida, p));
        var escolhida = _interacao.EscolherRepositorio(pastas, sugerida);
        if (escolhida is null) return;

        if (RegrasVinculo.AvisarPastaDivergente(lida, escolhida)
            && !_interacao.Confirmar("Retomar mesmo assim?",
                $"A pasta de trabalho desta sessão termina em \"{TextoExibicao.NomePasta(lida.Cwd)}\", mas o repositório escolhido é \"{escolhida}\". "
                + "A retomada pode não encontrar a conversa.", "Retomar"))
            return;

        var r = _abertura.RetomarNaoRegistrada(pedido, escolhida);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível retomar a sessão.");
    }

    /// <summary>Pastas diretas da raiz dos fontes com nome válido e que não são atalhos nem junções, em ordem.</summary>
    private List<string> PastasDaRaiz()
    {
        try
        {
            return new DirectoryInfo(_leitorTasks.Raiz).EnumerateDirectories()
                .Where(d => (d.Attributes & FileAttributes.ReparsePoint) == 0 && ValidadorEntrada.PastaValida(d.Name))
                .Select(d => d.Name)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>
    /// "Excluir": tira a sessão do painel de vez, depois de confirmar. A conversa e o arquivo de status não são tocados.
    /// </summary>
    private void ExcluirSessao(SessaoItem item)
    {
        if (!DadosDisponiveis) return;
        if (!_interacao.Confirmar("Excluir esta sessão?",
                "A conversa não é apagada, mas a sessão some do painel e não volta a aparecer.", "Excluir sessão"))
            return;

        var r = _dados.ExcluirSessao(item.AgenteDaSessao, item.SessionId, _relogio.GetUtcNow());
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível excluir a sessão.");
    }

    /// <summary>
    /// "Abrir" na linha de um lançamento pendente: abre o agente de novo no mesmo repositório, reaproveitando o
    /// lançamento (aba fechada antes da primeira mensagem). Se ele já não existir, nada é aberto e a linha some
    /// na atualização.
    /// </summary>
    private void AbrirLancamento(LancamentoItem item)
    {
        if (!DadosDisponiveis) return;
        var r = _abertura.ReabrirLancamento(item.Id);
        RecarregarEAplicar();
        // Só é erro se o lançamento continua no arquivo, ou seja, se a falha foi ao gravar ou ao iniciar o terminal.
        var continua = _estadoDados.Demandas.SelectMany(d => d.Repositorios).SelectMany(x => x.Lancamentos)
            .Any(l => string.Equals(l.Id, item.Id, StringComparison.OrdinalIgnoreCase));
        if (!r.Ok && continua) MostrarErro(r.Mensagem ?? "Não foi possível abrir a sessão.");
    }

    /// <summary>
    /// "Cancelar" na linha de um lançamento pendente: remove só o lançamento do arquivo de dados, sem confirmação e
    /// sem fechar a aba. Se ele já não existir (usado ou removido por outra janela), nada é gravado e a linha some
    /// na atualização.
    /// </summary>
    private void CancelarLancamento(LancamentoItem item)
    {
        if (!DadosDisponiveis) return;
        var r = _dados.CancelarLancamento(item.Id);
        RecarregarEAplicar();
        // Só é erro se o lançamento continua no arquivo, ou seja, se a gravação falhou.
        var continua = _estadoDados.Demandas.SelectMany(d => d.Repositorios).SelectMany(x => x.Lancamentos)
            .Any(l => string.Equals(l.Id, item.Id, StringComparison.OrdinalIgnoreCase));
        if (!r.Ok && continua) MostrarErro(r.Mensagem ?? "Não foi possível cancelar o lançamento.");
    }

    // ---------- vínculo de sessões de "Sessões" a um trabalho ----------

    /// <summary>"Vincular a um trabalho" no menu de uma sessão de "Sessões": diálogo com trabalho, repositório e rótulo.</summary>
    private void VincularSessao(SessaoItem item)
    {
        if (!DadosDisponiveis) return;

        // Uma sessão já registrada em "Sessões" (sem arquivo de status) vincula pelos dados do registro: só o agente
        // e o identificador do arquivo de dados viram o pedido; a pasta serve só para sugerir o repositório.
        var lida = item.Lida ?? (item.Registrada is { } r0
            ? new Sessao(r0.Agente, r0.SessionId, Path.Combine(_leitorTasks.Raiz, r0.Pasta), StatusGravado.Ativa, _relogio.GetUtcNow())
            : null);
        if (lida is null) return;

        var pedido = PedidoVinculo.Criar(lida);
        if (pedido is null)
        {
            MostrarErro(RegrasVinculo.PodeVincularSessao(lida).Motivo ?? "Esta sessão não pode ser vinculada.");
            return;
        }

        var opcoes = _estadoDados.Demandas
            .Where(d => !d.Arquivada && !RegrasDemanda.EhImplicita(d.Id))
            .Select(d => new OpcaoDemandaVinculo(
                d.Id,
                TextoExibicao.Limpar(d.Titulo),
                d.Repositorios.Select(r => r.Pasta).ToList(),
                d.Repositorios.Select(r => r.Pasta).FirstOrDefault(p => RegrasVinculo.PastaCoincide(lida, p))))
            .ToList();
        if (opcoes.Count == 0)
        {
            MostrarInformacao("Não há trabalho para receber a sessão. Crie um trabalho primeiro.");
            return;
        }

        var descricao = $"{(lida.Agente == Agente.Claude ? "Claude" : "Codex")}, pasta {TextoExibicao.NomePasta(lida.Cwd)}, sessão {TextoExibicao.Limpar(lida.SessionId)}";
        var vinculou = _interacao.DialogoVincularSessao(descricao, opcoes, escolha =>
        {
            if (!ValidadorEntrada.PastaValida(escolha.Repositorio))
                return "Repositório inválido: use apenas letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.";
            if (escolha.Rotulo is not null && !ValidadorEntrada.RotuloValido(escolha.Rotulo))
                return "Rótulo inválido: use até 40 caracteres, sem \";\" nem caracteres de controle.";

            if (RegrasVinculo.AvisarPastaDivergente(lida, escolha.Repositorio)
                && !_interacao.Confirmar("Vincular mesmo assim?",
                    $"A pasta de trabalho desta sessão termina em \"{TextoExibicao.NomePasta(lida.Cwd)}\", mas o repositório escolhido é \"{escolha.Repositorio}\". "
                    + "Ao retomar, o agente pode não encontrar a conversa.", "Vincular"))
                return ""; // não confirmou: o diálogo continua aberto

            var r = _dados.VincularSessao(pedido, escolha.DemandaId, escolha.Repositorio, escolha.Rotulo, _relogio.GetUtcNow());
            return r.Ok ? null : r.Mensagem ?? "Não foi possível vincular a sessão.";
        });
        if (vinculou) RecarregarEAplicar();
    }

    // ---------- tela ----------

    private void Receber(RetratoPasta retrato)
    {
        _retrato = retrato;
        RecarregarDados();
        // Sessões que correspondem a um lançamento pendente entram no card antes de a tela ser montada.
        if (retrato.Disponivel && VinculoAutomatico.Aplicar(_dados, _estadoDados, retrato.Sessoes, _relogio) > 0)
            RecarregarDados();
        Aplicar();
    }

    private void Aplicar()
    {
        if (_retrato is null) return;
        PastaIndisponivel = !_retrato.Disponivel;

        var visao = VisaoAgrupada.Montar(_estadoDados.Demandas, _retrato.Sessoes, MostrarArquivadas, MostrarAntigas, _relogio,
            _estadoDados.Excluidas);
        _visao = _retrato.Disponivel ? visao : null;
        AplicarAlerta(visao.ResumoCabecalho, _retrato.Disponivel);
        SemDemandas = visao.Demandas.Count == 0;

        // Tudo é atualizado no lugar, por chave: o item selecionado e a rolagem não se perdem
        // quando um status muda ou quando a ordem dos trabalhos muda.
        var linhas = visao.Demandas.Select(d => (Chave: Chave(d.Demanda.Id), Dado: (DemandaNaVisao?)d)).ToList();
        linhas.Add((DemandaItem.ChaveSessoes, null));

        SincronizacaoColecao.Sincronizar(Demandas, linhas,
            d => d.Chave, l => l.Chave,
            l => l.Dado is null ? _sessoes : NovoDemandaItem(l.Chave, false),
            (item, l) =>
            {
                if (l.Dado is { } d) item.Atualizar(d, NovoCard, NovaSessaoItem, NovoLancamentoItem);
                else item.AtualizarSessoes(visao.Sessoes, NovaSessaoItem, NovoLancamentoItem);
            });

        AtualizarProgresso();
        // Com filtro ativo, o texto dos trabalhos pode ter mudado o que casa; a seleção é reafirmada na lista.
        if (FiltroTexto.Length > 0) _vista.Refresh();
        OnPropertyChanged(nameof(DemandaSelecionada));
    }

    private void AplicarAlerta(ResumoCabecalho resumo, bool disponivel)
    {
        if (!disponivel)
        {
            AlertaNivel = NivelSinal.Neutro;
            AlertaTexto = "";
            PodeIrParaProxima = false;
            ContagemAtencao = "";
            ContagemAguardando = "";
            ContagemExecutando = "";
            return;
        }
        AlertaNivel = Sinal.De(resumo.StatusDestacado);
        AlertaTexto = resumo.TextoAlerta;
        PodeIrParaProxima = resumo.StatusDestacado is not null;
        // "aguardando você" aparece ao lado só quando o alerta é outro status (senão repetiria o alerta).
        ContagemAtencao = resumo.PrecisaAtencao > 0 ? resumo.TextoPrecisaAtencao : "";
        ContagemAguardando = resumo.Aguardando > 0 ? resumo.TextoAguardando : "";
        ContagemExecutando = resumo.Executando > 0 ? resumo.TextoExecutando : "";
    }

    public void Dispose() => _monitor.Dispose();
}
