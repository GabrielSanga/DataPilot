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
    private readonly DemandaItem _naoVinculadas;
    private readonly ICollectionView _vista;

    /// <summary>Todas as entradas da lista: demandas na ordem de urgência e, no fim, "Não vinculadas".</summary>
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
        _naoVinculadas = NovoDemandaItem(DemandaItem.ChaveNaoVinculadas, true);
        _vista = CollectionViewSource.GetDefaultView(Demandas);
        _vista.Filter = CasaComFiltro;
        _monitor = new MonitorPasta(pasta);
        Aviso = $"Pasta de status indisponível: {TextoExibicao.Limpar(pasta)}";
        _monitor.RetratoAtualizado += r => _dispatcher.BeginInvoke(() => Receber(r));
        RecarregarDados();
    }

    public void Iniciar() => _monitor.Iniciar();

    // Ao trocar de demanda o progresso aparece na hora, sem esperar o ciclo seguinte.
    partial void OnDemandaSelecionadaChanged(DemandaItem? value) => AtualizarProgresso();

    /// <summary>
    /// Relê o progresso da change só da demanda selecionada (a única que o mostra), a cada atualização da tela e
    /// ao trocar de demanda. Não há <c>FileSystemWatcher</c> nas pastas do <c>ia</c>.
    /// </summary>
    private void AtualizarProgresso()
    {
        if (DemandaSelecionada is not { Demanda: { } demanda } item) return;
        var progresso = ProgressoDemanda.Montar(demanda.OpenspecChange, demanda.Repositorios.Select(r => r.Pasta), _leitorTasks);
        item.AplicarProgresso(progresso);
    }

    partial void OnMostrarAntigasChanged(bool value) => Aplicar();
    partial void OnMostrarArquivadasChanged(bool value) => Aplicar();

    // ---------- filtro de demandas ----------

    private bool CasaComFiltro(object o)
        => o is not DemandaItem item || FiltroDemandas.Filtrar(new[] { item }, FiltroTexto, i => i.Demanda).Count == 1;

    partial void OnFiltroTextoChanged(string value) => AtualizarFiltro();

    /// <summary>
    /// Reaplica o filtro. Filtrar não muda a demanda selecionada: o aviso de que ela mudou só serve para a lista
    /// destacá-la de novo quando ela volta a aparecer.
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

    private DemandaItem NovoDemandaItem(string chave, bool naoVinculadas)
        => new(chave, naoVinculadas, EditarDemanda, ArquivarDemanda, DesarquivarDemanda, VincularRepositorio);

    private CardItem NovoCard(string pasta) => new(pasta, NovaSessao, RemoverRepositorio);

    private LancamentoItem NovoLancamentoItem(string id) => new(id, AbrirLancamento, CancelarLancamento);

    private SessaoItem NovaSessaoItem(string sessionId) => new(sessionId, Retomar, MoverSessao, RemoverSessao, VincularSessao);

    // ---------- dados do painel ----------

    private void RecarregarDados()
    {
        _estadoDados = _dados.Carregar();
        DadosDisponiveis = _estadoDados.Disponivel;
        AvisoDados = _estadoDados.Disponivel ? ""
            : $"Dados do painel indisponíveis: {TextoExibicao.Limpar(_dados.Caminho)} ({TextoExibicao.Limpar(_estadoDados.Erro ?? "")}). "
              + "O arquivo não foi alterado; criar e alterar demandas, abrir e retomar estão desabilitados.";
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

    /// <summary>"Ir para a próxima" e F8: seleciona a próxima demanda com sessão no status destacado no topo.</summary>
    [RelayCommand(CanExecute = nameof(PodeIrParaProxima))]
    private void IrParaProxima()
    {
        if (_visao is null) return;
        var destino = ProximaSessao.Escolher(_visao, DemandaSelecionada?.Demanda?.Id, DemandaSelecionada?.EhNaoVinculadas == true);
        if (destino is null) return;

        var chave = destino.EhNaoVinculadas ? DemandaItem.ChaveNaoVinculadas : Chave(destino.DemandaId!);
        var item = Demandas.FirstOrDefault(d => d.Chave == chave);
        if (item is null) return;
        // Se o filtro esconde o destino, ele é limpo: levar o usuário a algo que não aparece não ajuda.
        if (!CasaComFiltro(item)) FiltroTexto = "";
        DemandaSelecionada = item;
    }

    // ---------- demandas ----------

    public void NovaDemanda()
    {
        if (!DadosDisponiveis) return;
        string? criada = null;
        _interacao.DialogoDemanda(null, d =>
        {
            var r = _dados.CriarDemanda(d.Id, d.Titulo, d.OpenspecChange, _relogio.GetUtcNow());
            if (!r.Ok) return r.Mensagem;
            criada = d.Id;
            return null;
        });
        if (criada is null) return;
        RecarregarEAplicar();
        SelecionarPorChave(Chave(criada));
    }

    private void EditarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda || !RegrasDemanda.PodeEditar(demanda)) return;
        string? novoId = null;
        _interacao.DialogoDemanda(demanda, d =>
        {
            var r = _dados.EditarDemanda(demanda.Id, d.Id, d.Titulo, d.OpenspecChange);
            if (!r.Ok) return r.Mensagem;
            novoId = d.Id;
            return null;
        });
        if (novoId is null) return;
        RecarregarEAplicar();
        SelecionarPorChave(Chave(novoId)); // o id pode ter mudado, e com ele a chave do item
    }

    private void ArquivarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda) return;
        if (!RegrasDemanda.PodeArquivar(demanda))
        {
            MostrarErro(RegrasDemanda.EhImplicita(demanda.Id)
                ? "A demanda \"Sem demanda\" não pode ser arquivada." : "A demanda já está arquivada.");
            return;
        }
        if (item.TemSessaoEmAndamento
            && !_interacao.Confirmar("Arquivar a demanda?", "Esta demanda tem sessões em andamento.", "Arquivar"))
            return;
        var r = _dados.ArquivarDemanda(demanda.Id);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível arquivar a demanda.");
    }

    private void DesarquivarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda) return;
        var r = _dados.DesarquivarDemanda(demanda.Id);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível desarquivar a demanda.");
    }

    // ---------- repositórios ----------

    private void VincularRepositorio(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda || !RegrasDemanda.PodeVincularRepositorio(demanda)) return;

        // Sugere as pastas já usadas em outras demandas e ainda não vinculadas a esta.
        var jaNaDemanda = demanda.Repositorios.Select(r => r.Pasta).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sugestoes = _estadoDados.Demandas
            .SelectMany(d => d.Repositorios.Select(r => r.Pasta))
            .Where(p => !jaNaDemanda.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vinculou = _interacao.DialogoVincularRepositorio(item.Cabecalho, sugestoes, pasta =>
        {
            var r = _dados.VincularRepositorio(demanda.Id, pasta.Trim());
            return r.Ok ? null : r.Mensagem;
        });
        if (vinculou) RecarregarEAplicar();
    }

    private void RemoverRepositorio(CardItem card)
    {
        if (!DadosDisponiveis) return;
        if (card.Sessoes.Count > 0)
        {
            var n = card.Sessoes.Count;
            var msg = n == 1
                ? "A sessão registrada neste repositório deixará de poder ser retomada nesta demanda."
                : $"As {n} sessões registradas neste repositório deixarão de poder ser retomadas nesta demanda.";
            if (!_interacao.Confirmar($"Remover o repositório {card.Pasta}?", msg, "Remover repositório")) return;
        }
        var r = _dados.RemoverRepositorio(card.DemandaId, card.Pasta);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível remover o repositório.");
    }

    // ---------- abrir, retomar, mover e remover sessões ----------

    private void NovaSessao(CardItem card)
    {
        if (!DadosDisponiveis) return;
        var demanda = DemandaSelecionada?.Demanda;
        if (demanda is null || !RegrasDemanda.PodeAbrirNovaSessao(demanda)) return;

        var escolha = _interacao.PedirNovaSessao(DemandaSelecionada!.Cabecalho, card.Pasta);
        if (escolha is null) return;

        var r = _abertura.Nova(card.DemandaId, card.Pasta, escolha.Agente, escolha.Rotulo);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível abrir a sessão.");
    }

    private void Retomar(SessaoItem item)
    {
        if (!DadosDisponiveis || item.Registrada is not { } registrada) return;

        if (Correspondencia.ExigeConfirmacao(item.Status)
            && !_interacao.Confirmar("Abrir outra aba desta sessão?",
                $"Esta sessão aparece como \"{item.StatusTexto}\" e pode já estar aberta no terminal.", "Abrir outra aba"))
            return;

        var r = _abertura.Retomar(registrada);
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível retomar a sessão.");
    }

    private void MoverSessao(SessaoItem item)
    {
        if (!DadosDisponiveis || item.Registrada is not { } registrada) return;

        var destinos = _estadoDados.Demandas
            .Where(d => !d.Arquivada && !string.Equals(d.Id, registrada.DemandaId, StringComparison.OrdinalIgnoreCase))
            .Select(d => new DestinoDemanda(d.Id, $"{TextoExibicao.Limpar(d.Id)} — {TextoExibicao.Limpar(d.Titulo)}"))
            .ToList();
        if (destinos.Count == 0)
        {
            MostrarInformacao("Não há outra demanda para receber a sessão. Crie uma demanda primeiro.");
            return;
        }

        var destino = _interacao.EscolherDemandaDestino(destinos);
        if (destino is null) return;

        var r = _dados.MoverSessao(registrada.SessionId, destino);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível mover a sessão.");
    }

    private void RemoverSessao(SessaoItem item)
    {
        if (!DadosDisponiveis || item.Registrada is not { } registrada) return;
        if (!_interacao.Confirmar("Remover o registro desta sessão?",
                "A conversa não é apagada, mas a sessão deixa de poder ser retomada pelo painel.", "Remover registro"))
            return;

        var r = _dados.RemoverSessao(registrada.SessionId);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível remover o registro da sessão.");
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

    // ---------- vínculo de sessões não vinculadas ----------

    /// <summary>"Vincular" na entrada "Não vinculadas": diálogo com demanda, repositório e rótulo.</summary>
    private void VincularSessao(SessaoItem item)
    {
        if (!DadosDisponiveis || item.Lida is not { } lida) return;

        var pedido = PedidoVinculo.Criar(lida);
        if (pedido is null)
        {
            MostrarErro(RegrasVinculo.PodeVincularSessao(lida).Motivo ?? "Esta sessão não pode ser vinculada.");
            return;
        }

        var opcoes = _estadoDados.Demandas
            .Where(d => !d.Arquivada)
            .Select(d => new OpcaoDemandaVinculo(
                d.Id,
                $"{TextoExibicao.Limpar(d.Id)} — {TextoExibicao.Limpar(d.Titulo)}",
                d.Repositorios.Select(r => r.Pasta).ToList(),
                d.Repositorios.Select(r => r.Pasta).FirstOrDefault(p => RegrasVinculo.PastaCoincide(lida, p))))
            .ToList();
        if (opcoes.Count == 0)
        {
            MostrarInformacao("Não há demanda para receber a sessão. Crie uma demanda primeiro.");
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

        var visao = VisaoAgrupada.Montar(_estadoDados.Demandas, _retrato.Sessoes, MostrarArquivadas, MostrarAntigas, _relogio);
        _visao = _retrato.Disponivel ? visao : null;
        AplicarAlerta(visao.ResumoCabecalho, _retrato.Disponivel);
        SemDemandas = visao.Demandas.Count == 0;

        // Tudo é atualizado no lugar, por chave: a demanda selecionada e a rolagem não se perdem
        // quando um status muda ou quando a ordem das demandas muda.
        var linhas = visao.Demandas.Select(d => (Chave: Chave(d.Demanda.Id), Dado: (DemandaNaVisao?)d)).ToList();
        linhas.Add((DemandaItem.ChaveNaoVinculadas, null));

        SincronizacaoColecao.Sincronizar(Demandas, linhas,
            d => d.Chave, l => l.Chave,
            l => l.Dado is null ? _naoVinculadas : NovoDemandaItem(l.Chave, false),
            (item, l) =>
            {
                if (l.Dado is { } d) item.Atualizar(d, NovoCard, NovaSessaoItem, NovoLancamentoItem);
                else item.AtualizarNaoVinculadas(visao.NaoVinculadas, NovaSessaoItem);
            });

        AtualizarProgresso();
        // Com filtro ativo, o texto das demandas pode ter mudado o que casa; a seleção é reafirmada na lista.
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
            ContagemAguardando = "";
            ContagemExecutando = "";
            return;
        }
        AlertaNivel = Sinal.De(resumo.StatusDestacado);
        AlertaTexto = resumo.TextoAlerta;
        PodeIrParaProxima = resumo.StatusDestacado is not null;
        // "aguardando você" aparece ao lado só quando o alerta é outro status (senão repetiria o alerta).
        ContagemAguardando = resumo.StatusDestacado == StatusExibido.PrecisaAtencao && resumo.Aguardando > 0 ? resumo.TextoAguardando : "";
        ContagemExecutando = resumo.Executando > 0 ? resumo.TextoExecutando : "";
    }

    public void Dispose() => _monitor.Dispose();
}
