using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core.Display;
using DataPilot.Core.Models;
using DataPilot.Core.OpenSpec;
using DataPilot.Wpf.Helpers;

namespace DataPilot.Wpf.ViewModels;

/// <summary>
/// Entrada da lista de trabalhos, também usada para "Sessões" (<see cref="EhSessoes"/>).
/// Chave: o <c>id</c> do trabalho (que nunca é exibido); a da entrada especial é fixa.
/// </summary>
public sealed partial class DemandaItem : ObservableObject
{
    public const string ChaveSessoes = "\u0001sessoes";

    public string Chave { get; }
    public bool EhSessoes { get; }
    public Demanda? Demanda { get; private set; }
    public bool TemSessaoEmAndamento { get; private set; }

    public ObservableCollection<CardItem> Cards { get; } = [];
    /// <summary>Sessões da entrada "Sessões": registradas e não registradas, na ordem de urgência.</summary>
    public ObservableCollection<SessaoItem> Sessoes { get; } = [];
    public ObservableCollection<LancamentoItem> LancamentosSessoes { get; } = [];

    /// <summary>Título do trabalho (ou "Sessões"), para a lista, a área e os textos dos diálogos.</summary>
    [ObservableProperty] private string _titulo = "";
    /// <summary>Contagens em frase, como "2 repositórios, 3 sessões".</summary>
    [ObservableProperty] private string _resumo = "";
    /// <summary>Nome do status mais urgente, só quando ele acende (precisa de atenção ou aguardando você).</summary>
    [ObservableProperty] private string _destaqueTexto = "";
    [ObservableProperty] private bool _temDestaque;
    [ObservableProperty] private NivelSinal _nivel;
    [ObservableProperty] private bool _arquivada;
    [ObservableProperty] private bool _semRepositorios;
    [ObservableProperty] private bool _semSessoes;
    [ObservableProperty] private bool _podeEditar;
    [ObservableProperty] private bool _podeArquivar;
    [ObservableProperty] private bool _podeDesarquivar;
    [ObservableProperty] private bool _podeVincular;

    public IRelayCommand EditarCommand { get; }
    public IRelayCommand ArquivarCommand { get; }
    public IRelayCommand DesarquivarCommand { get; }
    public IRelayCommand VincularCommand { get; }
    public IRelayCommand NovaSessaoCommand { get; }

    public DemandaItem(string chave, bool sessoes,
        Action<DemandaItem> editar, Action<DemandaItem> arquivar, Action<DemandaItem> desarquivar, Action<DemandaItem> vincular,
        Action<DemandaItem> novaSessao)
    {
        Chave = chave;
        EhSessoes = sessoes;
        EditarCommand = new RelayCommand(() => editar(this));
        ArquivarCommand = new RelayCommand(() => arquivar(this));
        DesarquivarCommand = new RelayCommand(() => desarquivar(this));
        VincularCommand = new RelayCommand(() => vincular(this));
        NovaSessaoCommand = new RelayCommand(() => novaSessao(this));
    }

    public void Atualizar(DemandaNaVisao v, Func<string, CardItem> criarCard, Func<string, SessaoItem> criarSessao,
        Func<string, LancamentoItem> criarLancamento)
    {
        var d = v.Demanda;
        Demanda = d;
        TemSessaoEmAndamento = v.TemSessaoEmAndamento;
        Titulo = TextoExibicao.Limpar(d.Titulo);
        Resumo = v.Resumo + (d.Arquivada ? ", arquivado" : "");
        AplicarSinal(v.MaisUrgente);
        Arquivada = d.Arquivada;
        SemRepositorios = v.Cards.Count == 0;
        PodeEditar = RegrasDemanda.PodeEditar(d);
        PodeArquivar = RegrasDemanda.PodeArquivar(d);
        PodeDesarquivar = RegrasDemanda.PodeDesarquivar(d);
        PodeVincular = RegrasDemanda.PodeVincularRepositorio(d);

        SincronizacaoColecao.Sincronizar(Cards, v.Cards,
            c => c.Pasta, c => c.Pasta,
            c => criarCard(c.Pasta),
            (item, dado) => item.Atualizar(dado, d, criarSessao, criarLancamento));
    }

    /// <summary>Mostra o progresso da change em cada card. Não há soma no topo do trabalho.</summary>
    public void AplicarProgresso(IReadOnlyDictionary<string, ProgressoRepositorio> progresso)
    {
        foreach (var card in Cards)
            card.AplicarProgresso(progresso.TryGetValue(card.Pasta, out var p) ? p : null);
    }

    /// <summary>Faixa e nome do status só quando o mais urgente é um dos dois que precisam do usuário.</summary>
    private void AplicarSinal(StatusExibido? maisUrgente)
    {
        Nivel = Sinal.De(maisUrgente);
        TemDestaque = Sinal.Acende(maisUrgente);
        DestaqueTexto = TemDestaque ? RegrasExibicao.Texto(maisUrgente!.Value) : "";
    }

    /// <summary>Uma linha da entrada "Sessões": registrada na <c>sem-demanda</c> ou ainda não registrada.</summary>
    private sealed record LinhaSessoes(string Id, SessaoNaVisao? Registrada, SessaoExibida? NaoRegistrada)
    {
        public StatusExibido Status => Registrada?.Status ?? NaoRegistrada!.Status;
        public DateTimeOffset Data => Registrada is { } r ? r.Lida?.Atualizado ?? r.Registrada.CriadaEm : NaoRegistrada!.Sessao.Atualizado;
    }

    public void AtualizarSessoes(EntradaSessoes e, Func<string, SessaoItem> criarSessao, Func<string, LancamentoItem> criarLancamento)
    {
        Titulo = "Sessões";
        Resumo = e.Quantidade switch { 0 => "nenhuma sessão", 1 => "1 sessão", var n => $"{n} sessões" };
        AplicarSinal(e.MaisUrgente);
        SemSessoes = e.Quantidade == 0 && e.Lancamentos.Count == 0;

        var linhas = e.Registradas.Select(r => new LinhaSessoes(r.Registrada.SessionId, r, null))
            .Concat(e.NaoRegistradas.Select(n => new LinhaSessoes(n.Sessao.SessionId, null, n)))
            .OrderBy(l => l.Status)
            .ThenByDescending(l => l.Data)
            .ThenBy(l => l.Id, StringComparer.Ordinal)
            .ToList();
        SincronizacaoColecao.Sincronizar(Sessoes, linhas,
            s => s.SessionId, l => l.Id,
            l => criarSessao(l.Id),
            (item, l) =>
            {
                if (l.Registrada is { } r) item.Atualizar(r, emSessoes: true);
                else item.AtualizarNaoRegistrada(l.NaoRegistrada!);
            });
        SincronizacaoColecao.Sincronizar(LancamentosSessoes, e.Lancamentos,
            l => l.Id, l => l.Lancamento.Lancamento.Id,
            l => criarLancamento(l.Lancamento.Lancamento.Id),
            (item, l) => item.Atualizar(l.Lancamento, l.Pasta, emSessoes: true));
    }
}
