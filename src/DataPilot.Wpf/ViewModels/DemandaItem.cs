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

    private readonly MainViewModel _painel;

    internal DemandaItem(string chave, bool sessoes, MainViewModel painel)
    {
        Chave = chave;
        EhSessoes = sessoes;
        _painel = painel;
        EditarCommand = new RelayCommand(() => _painel.EditarDemanda(this));
        ArquivarCommand = new RelayCommand(() => _painel.ArquivarDemanda(this));
        DesarquivarCommand = new RelayCommand(() => _painel.DesarquivarDemanda(this));
        VincularCommand = new RelayCommand(() => _painel.VincularRepositorio(this));
        NovaSessaoCommand = new RelayCommand(() => _painel.NovaSessaoEmSessoes(this));
    }

    public void Atualizar(DemandaNaVisao v)
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
            c => new CardItem(c.Pasta, _painel),
            (item, dado) => item.Atualizar(dado, d));
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

    public void AtualizarSessoes(EntradaSessoes e)
    {
        Titulo = "Sessões";
        Resumo = RegrasExibicao.Quantidade(e.Quantidade, "sessão", "sessões", "nenhuma");
        AplicarSinal(e.MaisUrgente);
        SemSessoes = e.Quantidade == 0 && e.Lancamentos.Count == 0;

        var linhas = e.Registradas.Select(r => new LinhaSessoes(r.Registrada.SessionId, r, null))
            .Concat(e.NaoRegistradas.Select(n => new LinhaSessoes(n.Sessao.SessionId, null, n)))
            .ToList();
        linhas.Sort((a, b) => RegrasExibicao.ComparaSessoes((a.Status, a.Data, a.Id), (b.Status, b.Data, b.Id)));
        SincronizacaoColecao.Sincronizar(Sessoes, linhas,
            s => s.SessionId, l => l.Id,
            l => new SessaoItem(l.Id, _painel),
            (item, l) =>
            {
                if (l.Registrada is { } r) item.Atualizar(r, emSessoes: true);
                else item.AtualizarNaoRegistrada(l.NaoRegistrada!);
            });
        SincronizacaoColecao.Sincronizar(LancamentosSessoes, e.Lancamentos,
            l => l.Id, l => l.Lancamento.Lancamento.Id,
            l => new LancamentoItem(l.Lancamento.Lancamento.Id, _painel),
            (item, l) => item.Atualizar(l.Lancamento, l.Pasta, emSessoes: true));
    }
}
