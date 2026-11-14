using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
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
    private RetratoPasta? _retrato;
    private EstadoDados _estadoDados = EstadoDados.Vazio;

    public ObservableCollection<SessaoItem> Sessoes { get; } = [];
    public ObservableCollection<RepositorioCadastrado> Repositorios { get; } = [];

    [ObservableProperty] private string _resumo = "Carregando…";
    [ObservableProperty] private bool _pastaIndisponivel;
    [ObservableProperty] private string _aviso = "";
    [ObservableProperty] private bool _semSessoes;
    [ObservableProperty] private bool _mostrarAntigas;
    [ObservableProperty] private bool _dadosDisponiveis = true;
    [ObservableProperty] private string _avisoDados = "";

    public MainViewModel(string pasta, TimeProvider relogio, Dispatcher dispatcher,
        DadosPainel dados, AberturaSessao abertura, IInteracao interacao)
    {
        _relogio = relogio;
        _dispatcher = dispatcher;
        _dados = dados;
        _abertura = abertura;
        _interacao = interacao;
        _monitor = new MonitorPasta(pasta);
        Aviso = $"Pasta de status indisponível: {TextoExibicao.Limpar(pasta)}";
        _monitor.RetratoAtualizado += r => _dispatcher.BeginInvoke(() => Receber(r));
        RecarregarDados();
    }

    public void Iniciar() => _monitor.Iniciar();

    partial void OnMostrarAntigasChanged(bool value) => Aplicar();

    // ---------- dados do painel ----------

    private void RecarregarDados()
    {
        _estadoDados = _dados.Carregar();
        DadosDisponiveis = _estadoDados.Disponivel;
        AvisoDados = _estadoDados.Disponivel ? ""
            : $"Dados do painel indisponíveis: {TextoExibicao.Limpar(_dados.Caminho)} ({TextoExibicao.Limpar(_estadoDados.Erro ?? "")}). "
              + "O arquivo não foi alterado; cadastrar, abrir e retomar estão desabilitados.";

        // Só recria a coleção quando o conteúdo muda: esta releitura acontece a cada poucos segundos,
        // e recriar sempre apagaria a seleção dos diálogos abertos.
        if (Assinatura(Repositorios) == Assinatura(_estadoDados.Repositorios)) return;
        Repositorios.Clear();
        foreach (var r in _estadoDados.Repositorios) Repositorios.Add(r);
    }

    private static string Assinatura(IEnumerable<RepositorioCadastrado> repositorios)
        => string.Join("\n", repositorios.Select(r =>
            r.Pasta + "|" + string.Join(",", r.Sessoes.Select(s => $"{s.Agente}:{s.SessionId}:{s.Rotulo}"))));

    private void RecarregarEAplicar()
    {
        RecarregarDados();
        Aplicar();
    }

    /// <summary>Cadastra o repositório; devolve a mensagem de recusa ou null se deu certo.</summary>
    public string? Cadastrar(string pasta)
    {
        var r = _dados.Cadastrar(pasta.Trim());
        RecarregarEAplicar();
        return r.Ok ? null : r.Mensagem;
    }

    /// <summary>Remove o repositório, pedindo confirmação quando há sessões registradas.</summary>
    public string? Remover(RepositorioCadastrado repo)
    {
        if (repo.Sessoes.Count > 0)
        {
            var n = repo.Sessoes.Count;
            var msg = $"O repositório {repo.Pasta} tem {n} {(n == 1 ? "sessão registrada que deixará" : "sessões registradas que deixarão")} de poder ser retomada{(n == 1 ? "" : "s")}. Remover mesmo assim?";
            if (!_interacao.Confirmar(msg)) return null;
        }
        var r = _dados.Remover(repo.Pasta);
        RecarregarEAplicar();
        return r.Ok ? null : r.Mensagem;
    }

    // ---------- abrir e retomar ----------

    public ResultadoOperacao NovaSessao(string pasta, Agente agente, string? rotulo)
    {
        var r = _abertura.Nova(pasta, agente, rotulo);
        RecarregarEAplicar();
        return r;
    }

    public void AvisarSemRepositorios()
        => _interacao.Informar("Cadastre um repositório antes de abrir uma sessão (botão \"Repositórios\").");

    public void MostrarErro(string mensagem) => _interacao.Erro(mensagem);

    private void Retomar(SessaoItem item)
    {
        if (!DadosDisponiveis || item.Registrada is not { } registrada) return;

        if (Correspondencia.ExigeConfirmacao(item.Status)
            && !_interacao.Confirmar($"Esta sessão aparece como \"{item.StatusTexto}\" e pode já estar aberta no terminal. Abrir outra aba mesmo assim?"))
            return;

        var r = _abertura.Retomar(registrada);
        if (!r.Ok) _interacao.Erro(r.Mensagem ?? "Não foi possível retomar a sessão.");
    }

    // ---------- lista ----------

    private void Receber(RetratoPasta retrato)
    {
        _retrato = retrato;
        RecarregarDados();
        Aplicar();
    }

    private void Aplicar()
    {
        if (_retrato is null) return;
        PastaIndisponivel = !_retrato.Disponivel;

        var alvo = RegrasExibicao.Montar(_retrato.Sessoes, MostrarAntigas, _relogio);
        Resumo = _retrato.Disponivel ? RegrasExibicao.Resumo(alvo) : "";
        SemSessoes = _retrato.Disponivel && alvo.Count == 0;
        var repos = _estadoDados.Repositorios;

        // Atualiza a coleção no lugar, por identificador, para preservar seleção e rolagem.
        var ids = alvo.Select(a => a.Sessao.SessionId).ToHashSet();
        for (var i = Sessoes.Count - 1; i >= 0; i--)
            if (!ids.Contains(Sessoes[i].SessionId)) Sessoes.RemoveAt(i);

        for (var i = 0; i < alvo.Count; i++)
        {
            var e = alvo[i];
            var registrada = Correspondencia.Registrada(e.Sessao, repos);
            var atual = -1;
            for (var j = i; j < Sessoes.Count; j++)
                if (Sessoes[j].SessionId == e.Sessao.SessionId) { atual = j; break; }

            if (atual < 0) Sessoes.Insert(i, new SessaoItem(e, registrada, Retomar));
            else
            {
                if (atual != i) Sessoes.Move(atual, i);
                Sessoes[i].Atualizar(e, registrada);
            }
        }
    }

    public void Dispose() => _monitor.Dispose();
}
