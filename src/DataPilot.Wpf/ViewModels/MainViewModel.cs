using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core.Display;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Monitoring;
using DataPilot.Core.OpenSpec;
using DataPilot.Core.Persistence;
using DataPilot.Core.Sessions;
using DataPilot.Core.Validation;
using DataPilot.Wpf.Helpers;
using DataPilot.Wpf.Services;

namespace DataPilot.Wpf.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly MonitorPasta _monitor;
    private readonly TimeProvider _relogio;
    private readonly Dispatcher _dispatcher;
    private readonly DadosPainel _dados;
    private readonly string _pastaStatus;
    private readonly AberturaSessao _abertura;
    private readonly IInteracao _interacao;
    private readonly LeitorTasks _leitorTasks;
    private RetratoPasta? _retrato;
    private EstadoDados _estadoDados = EstadoDados.Vazio;
    private readonly DemandaItem _sessoes;
    private readonly ICollectionView _vista;
    public ObservableCollection<DemandaItem> Demandas { get; } = [];
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
    [ObservableProperty] private NivelSinal _alertaNivel = NivelSinal.Neutro;
    [ObservableProperty] private string _alertaTexto = "Carregando…";
    [ObservableProperty] private string _contagemAtencao = "";
    [ObservableProperty] private string _contagemAguardando = "";
    [ObservableProperty] private string _contagemExecutando = "";
    [ObservableProperty] private string _mensagem = "";
    [ObservableProperty] private bool _mensagemEhErro;
    [ObservableProperty] private bool _temMensagem;

    public MainViewModel(string pasta, TimeProvider relogio, Dispatcher dispatcher, DadosPainel dados, AberturaSessao abertura, IInteracao interacao, LeitorTasks leitorTasks)
    {
        _leitorTasks = leitorTasks;
        _relogio = relogio;
        _dispatcher = dispatcher;
        _dados = dados;
        _abertura = abertura;
        _interacao = interacao;
        _sessoes = new DemandaItem(DemandaItem.ChaveSessoes, true, this);
        _vista = CollectionViewSource.GetDefaultView(Demandas);
        _vista.Filter = CasaComFiltro;
        _pastaStatus = pasta;
        _monitor = new MonitorPasta(pasta);
        Aviso = $"Pasta de status indisponível: {TextoExibicao.Limpar(pasta)}";
        _monitor.RetratoAtualizado += r => _dispatcher.BeginInvoke(() => Receber(r));
        RecarregarDados();
    }

    public void Iniciar() => _monitor.Iniciar();


    public void AbrirConfiguracoes()
    {
        var estado = _dados.Carregar();
        var semSalvar = estado.Disponivel ? null : $"Não é possível salvar: o arquivo de dados está ilegível ({TextoExibicao.Limpar(estado.Erro ?? "")}).";
        _interacao.DialogoConfiguracoes(_pastaStatus, estado.Configuracoes, semSalvar, c =>
        {
            var r = _dados.SalvarConfiguracoes(c);
            return r.Ok ? null : r.Mensagem;
        });
    }

    // -------------------- DEMANDA -------------------------- \\

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

    internal void EditarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda || !RegrasDemanda.PodeEditar(demanda)) return;
        var editou = _interacao.DialogoTrabalho(demanda.Titulo, titulo =>
        {
            var r = _dados.EditarDemanda(demanda.Id, titulo);
            return r.Ok ? null : r.Mensagem;
        });
        if (editou) RecarregarEAplicar();
    }

    internal void ArquivarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda) return;
        if (!RegrasDemanda.PodeArquivar(demanda))
        {
            MostrarErro(RegrasDemanda.EhImplicita(demanda.Id)
                ? "\"Sessões\" não pode ser arquivada." : "O trabalho já está arquivado.");
            return;
        }
        if (item.TemSessaoEmAndamento && !_interacao.Confirmar("Arquivar o trabalho?", "Este trabalho tem sessões em andamento.", "Arquivar"))
            return;
        var r = _dados.ArquivarDemanda(demanda.Id);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível arquivar o trabalho.");
    }

    internal void DesarquivarDemanda(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda) return;
        var r = _dados.DesarquivarDemanda(demanda.Id);
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível desarquivar o trabalho.");
    }

    partial void OnDemandaSelecionadaChanged(DemandaItem? value) => AtualizarProgresso();

    private bool CasaComFiltro(object o) => o is not DemandaItem { Demanda: { } demanda } || FiltroDemandas.Casa(demanda, FiltroTexto);

    partial void OnFiltroTextoChanged(string value) => AtualizarFiltro();

    private void AtualizarFiltro()
    {
        _vista.Refresh();
        OnPropertyChanged(nameof(DemandaSelecionada));
    }

    //--------------------- REPOSITORIO ---------------------\\

    internal void VincularRepositorio(DemandaItem item)
    {
        if (!DadosDisponiveis || item.Demanda is not { } demanda || !RegrasDemanda.PodeVincularRepositorio(demanda)) return;

        var vinculou = _interacao.DialogoRepositorio(_leitorTasks.Raiz, null, null, (pasta, change) =>
        {
            var r = _dados.VincularRepositorio(demanda.Id, pasta, change);
            return r.Ok ? null : r.Mensagem;
        });
        if (vinculou) RecarregarEAplicar();
    }

    internal void RemoverRepositorio(CardItem card)
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

    internal void TrocarChange(CardItem card)
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

    // -------------------- OPENSPEC -------------------------- \\

    private void AtualizarProgresso()
    {
        if (DemandaSelecionada is not { Demanda: { } demanda } item) return;
        item.AplicarProgresso(_leitorTasks.LerRepositorios(demanda.Repositorios));
    }

    partial void OnMostrarAntigasChanged(bool value) => Aplicar();

    partial void OnMostrarArquivadasChanged(bool value) => Aplicar();

    // -------------------- DADOS PAINEL ---------------------\\

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

    private void SelecionarPorChave(string chave) => DemandaSelecionada = Demandas.FirstOrDefault(d => d.Chave == chave);

    private static string Chave(string demandaId) => demandaId.ToLowerInvariant();

    // --------------------- SESSÕES -------------------------- \\

    // A verificação do container pode levar segundos: a abertura roda fora da thread de interface, e o comando
    // assíncrono mantém o botão desabilitado até ela terminar.
    private static Task<ResultadoOperacao> Abrir(Func<ResultadoOperacao> abertura) => Task.Run(abertura);

    internal async Task NovaSessao(CardItem card)
    {
        if (!DadosDisponiveis) return;
        var demanda = DemandaSelecionada?.Demanda;
        if (demanda is null || !RegrasDemanda.PodeAbrirNovaSessao(demanda)) return;

        var escolha = _interacao.PedirNovaSessao();
        if (escolha is null) return;

        var r = await Abrir(() => _abertura.Nova(card.DemandaId, card.Pasta, escolha.Agente, escolha.Rotulo));
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível abrir a sessão.");
    }

    internal async Task NovaSessaoEmSessoes(DemandaItem item)
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

        var r = await Abrir(() => _abertura.NovaEmSessoes(pasta.Pasta!, escolha.Agente, escolha.Rotulo));
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível abrir a sessão.");
    }

    internal async Task Retomar(SessaoItem item)
    {
        if (!DadosDisponiveis) return;

        if (RegrasExibicao.ExigeConfirmacao(item.Status)
            && !_interacao.Confirmar("Abrir outra aba desta sessão?",
                $"Esta sessão aparece como \"{item.StatusTexto}\" e pode já estar aberta no terminal.", "Abrir outra aba"))
            return;

        if (item.Registrada is { } registrada)
        {
            var r = await Abrir(() => _abertura.Retomar(registrada));
            if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível retomar a sessão.");
            return;
        }
        if (item.Lida is { } lida) await RetomarNaoRegistrada(lida);
    }

    private async Task RetomarNaoRegistrada(Sessao lida)
    {
        var pedido = PedidoVinculo.Criar(lida);
        if (pedido is null)
        {
            MostrarErro(RegrasVinculo.MotivoIdentificadorInvalido);
            return;
        }

        var pastas = EscolhaDePasta.PastasDaRaiz(_leitorTasks.Raiz);
        if (pastas.Count == 0)
        {
            MostrarErro($"Nenhuma pasta encontrada em {TextoExibicao.Limpar(_leitorTasks.Raiz)}.");
            return;
        }
        var sugerida = pastas.FirstOrDefault(p => RegrasVinculo.PastaCoincide(lida.Cwd, p));
        var escolhida = _interacao.EscolherRepositorio(pastas, sugerida);
        if (escolhida is null) return;

        if (RegrasVinculo.AvisarPastaDivergente(lida.Cwd, escolhida)
            && !_interacao.Confirmar("Retomar mesmo assim?",
                $"A pasta de trabalho desta sessão termina em \"{TextoExibicao.NomePasta(lida.Cwd)}\", mas o repositório escolhido é \"{escolhida}\". "
                + "A retomada pode não encontrar a conversa.", "Retomar"))
            return;

        var r = await Abrir(() => _abertura.RetomarNaoRegistrada(pedido, escolhida));
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível retomar a sessão.");
    }

    internal void ExcluirSessao(SessaoItem item)
    {
        if (!DadosDisponiveis) return;
        if (!_interacao.Confirmar("Excluir esta sessão?",
                "A conversa não é apagada, mas a sessão some do painel e não volta a aparecer.", "Excluir sessão"))
            return;

        var r = _dados.ExcluirSessao(item.AgenteDaSessao, item.SessionId, _relogio.GetUtcNow());
        RecarregarEAplicar();
        if (!r.Ok) MostrarErro(r.Mensagem ?? "Não foi possível excluir a sessão.");
    }

    private bool LancamentoExiste(string id)
        => _estadoDados.Demandas.SelectMany(d => d.Repositorios).SelectMany(x => x.Lancamentos)
            .Any(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));

    internal async Task AbrirLancamento(LancamentoItem item)
    {
        if (!DadosDisponiveis) return;
        var r = await Abrir(() => _abertura.ReabrirLancamento(item.Id));
        RecarregarEAplicar();
        // Só é erro se o lançamento continua no arquivo, ou seja, se a falha foi ao gravar ou ao iniciar o terminal.
        if (!r.Ok && LancamentoExiste(item.Id)) MostrarErro(r.Mensagem ?? "Não foi possível abrir a sessão.");
    }

    internal void CancelarLancamento(LancamentoItem item)
    {
        if (!DadosDisponiveis) return;
        var r = _dados.DesfazerLancamento(item.Id);
        RecarregarEAplicar();
        // Só é erro se o lançamento continua no arquivo, ou seja, se a gravação falhou.
        if (!r.Ok && LancamentoExiste(item.Id)) MostrarErro(r.Mensagem ?? "Não foi possível cancelar o lançamento.");
    }

    internal void VincularSessao(SessaoItem item)
    {
        if (!DadosDisponiveis) return;

        // Uma sessão já registrada em "Sessões" (sem arquivo de status) vincula pelos dados do registro: só o agente
        // e o identificador do arquivo de dados viram o pedido; a pasta serve só para sugerir o repositório.
        Agente agente;
        string sessionId, cwd;
        PedidoVinculo? pedido;
        if (item.Lida is { } lida)
            (agente, sessionId, cwd, pedido) = (lida.Agente, lida.SessionId, lida.Cwd, PedidoVinculo.Criar(lida));
        else if (item.Registrada is { } registrada)
            (agente, sessionId, cwd, pedido) = (registrada.Agente, registrada.SessionId,
                Path.Combine(_leitorTasks.Raiz, registrada.Pasta), PedidoVinculo.Criar(registrada));
        else return;

        if (pedido is null)
        {
            MostrarErro(RegrasVinculo.MotivoIdentificadorInvalido);
            return;
        }

        var opcoes = _estadoDados.Demandas
            .Where(d => !d.Arquivada && !RegrasDemanda.EhImplicita(d.Id))
            .Select(d => new OpcaoDemandaVinculo(
                d.Id,
                TextoExibicao.Limpar(d.Titulo),
                d.Repositorios.Select(r => r.Pasta).ToList(),
                d.Repositorios.Select(r => r.Pasta).FirstOrDefault(p => RegrasVinculo.PastaCoincide(cwd, p))))
            .ToList();
        if (opcoes.Count == 0)
        {
            MostrarInformacao("Não há trabalho para receber a sessão. Crie um trabalho primeiro.");
            return;
        }

        var descricao = $"{Agentes.Nome(agente)}, pasta {TextoExibicao.NomePasta(cwd)}, sessão {TextoExibicao.Limpar(sessionId)}";
        var vinculou = _interacao.DialogoVincularSessao(descricao, opcoes, escolha =>
        {
            if (RegrasVinculo.AvisarPastaDivergente(cwd, escolha.Repositorio)
                && !_interacao.Confirmar("Vincular mesmo assim?",
                    $"A pasta de trabalho desta sessão termina em \"{TextoExibicao.NomePasta(cwd)}\", mas o repositório escolhido é \"{escolha.Repositorio}\". "
                    + "Ao retomar, o agente pode não encontrar a conversa.", "Vincular"))
                return ""; // não confirmou: o diálogo continua aberto

            var r = _dados.VincularSessao(pedido, escolha.DemandaId, escolha.Repositorio, escolha.Rotulo, _relogio.GetUtcNow());
            return r.Ok ? null : r.Mensagem ?? "Não foi possível vincular a sessão.";
        });
        if (vinculou) RecarregarEAplicar();
    }

    // -------------------- MENSAGENS ----------------------- \\

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

    // -------------------- TELA ------------------------------- \\

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

        var visao = VisaoAgrupada.Montar(_estadoDados.Demandas, _retrato.Sessoes, MostrarArquivadas, MostrarAntigas, _relogio, _estadoDados.Excluidas);
        AplicarAlerta(visao.ResumoCabecalho, _retrato.Disponivel);
        SemDemandas = visao.Demandas.Count == 0;

        // Tudo é atualizado no lugar, por chave: o item selecionado e a rolagem não se perdem
        // quando um status muda ou quando a ordem dos trabalhos muda.
        var linhas = visao.Demandas.Select(d => (Chave: Chave(d.Demanda.Id), Dado: (DemandaNaVisao?)d)).ToList();
        linhas.Add((DemandaItem.ChaveSessoes, null));

        SincronizacaoColecao.Sincronizar(Demandas, linhas,
            d => d.Chave, l => l.Chave,
            l => l.Dado is null ? _sessoes : new DemandaItem(l.Chave, false, this),
            (item, l) =>
            {
                if (l.Dado is { } d) item.Atualizar(d);
                else item.AtualizarSessoes(visao.Sessoes);
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
            ContagemAtencao = "";
            ContagemAguardando = "";
            ContagemExecutando = "";
            return;
        }
        AlertaNivel = Sinal.De(resumo.StatusDestacado);
        AlertaTexto = resumo.TextoAlerta;
        // "aguardando você" aparece ao lado só quando o alerta é outro status (senão repetiria o alerta).
        ContagemAtencao = resumo.PrecisaAtencao > 0 ? resumo.TextoPrecisaAtencao : "";
        ContagemAguardando = resumo.Aguardando > 0 ? resumo.TextoAguardando : "";
        ContagemExecutando = resumo.Executando > 0 ? resumo.TextoExecutando : "";
    }

    public void Dispose() => _monitor.Dispose();
}
