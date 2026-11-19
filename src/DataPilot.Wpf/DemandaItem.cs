using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataPilot.Core;

namespace DataPilot.Wpf;

/// <summary>Atualiza uma coleção no lugar, por chave, para preservar seleção e rolagem (nada é recriado).</summary>
public static class SincronizacaoColecao
{
    public static void Sincronizar<TItem, TDado>(
        ObservableCollection<TItem> colecao,
        IReadOnlyList<TDado> dados,
        Func<TItem, string> chaveItem,
        Func<TDado, string> chaveDado,
        Func<TDado, TItem> criar,
        Action<TItem, TDado> atualizar)
    {
        var chaves = dados.Select(chaveDado).ToHashSet();
        for (var i = colecao.Count - 1; i >= 0; i--)
            if (!chaves.Contains(chaveItem(colecao[i]))) colecao.RemoveAt(i);

        for (var i = 0; i < dados.Count; i++)
        {
            var chave = chaveDado(dados[i]);
            var atual = -1;
            for (var j = i; j < colecao.Count; j++)
                if (chaveItem(colecao[j]) == chave) { atual = j; break; }

            if (atual < 0)
            {
                var novo = criar(dados[i]);
                atualizar(novo, dados[i]); // já preenchido quando entra na lista
                colecao.Insert(i, novo);
                continue;
            }
            if (atual != i) colecao.Move(atual, i);
            atualizar(colecao[i], dados[i]);
        }
    }
}

/// <summary>
/// Linha de um lançamento pendente: uma sessão nova do Codex ainda sem a primeira mensagem. Não é uma sessão:
/// só tem "Abrir" (abre o agente de novo, reaproveitando o lançamento) e "Cancelar". Chave: o identificador do
/// lançamento. Todo texto vem limpo do Core e é exibido como texto simples.
/// </summary>
public sealed partial class LancamentoItem : ObservableObject
{
    public string Id { get; }

    [ObservableProperty] private string _agente = "";
    [ObservableProperty] private string _rotulo = "";
    /// <summary>Repositório do lançamento, mostrado só em "Sessões".</summary>
    [ObservableProperty] private string _pasta = "";
    [ObservableProperty] private bool _emSessoes;
    [ObservableProperty] private string _tempo = "";

    public IRelayCommand AbrirCommand { get; }
    public IRelayCommand CancelarCommand { get; }

    public LancamentoItem(string id, Action<LancamentoItem> abrir, Action<LancamentoItem> cancelar)
    {
        Id = id;
        AbrirCommand = new RelayCommand(() => abrir(this));
        CancelarCommand = new RelayCommand(() => cancelar(this));
    }

    public void Atualizar(LancamentoNaVisao v, string pasta, bool emSessoes)
    {
        var l = v.Lancamento;
        Agente = l.Agente == DataPilot.Core.Agente.Claude ? "Claude" : "Codex";
        Rotulo = l.Rotulo is null ? "" : TextoExibicao.Limpar(l.Rotulo);
        Pasta = TextoExibicao.Limpar(pasta);
        EmSessoes = emSessoes;
        Tempo = v.Tempo.Length == 0 ? "—" : v.Tempo;
    }
}

/// <summary>Card de um repositório dentro de um trabalho. Chave: a pasta (única dentro do trabalho).</summary>
public sealed partial class CardItem : ObservableObject
{
    public string Pasta { get; }
    public string DemandaId { get; private set; } = "";
    public ObservableCollection<SessaoItem> Sessoes { get; } = [];
    public ObservableCollection<LancamentoItem> Lancamentos { get; } = [];

    [ObservableProperty] private bool _vazio;

    /// <summary>Change do OpenSpec deste repositório, própria ou herdada; vazio quando não há.</summary>
    [ObservableProperty] private string _change = "";
    [ObservableProperty] private bool _temChange;
    /// <summary>Progresso da change ("4 de 6") ou a indicação que o substitui; vazio quando não há o que mostrar.</summary>
    [ObservableProperty] private string _progressoTexto = "";
    [ObservableProperty] private bool _temProgresso;
    [ObservableProperty] private bool _temBarra;
    [ObservableProperty] private double _progressoFracao;

    [ObservableProperty] private bool _podeNovaSessao;
    public IRelayCommand NovaSessaoCommand { get; }
    public IRelayCommand TrocarChangeCommand { get; }
    public IRelayCommand RemoverRepositorioCommand { get; }

    public CardItem(string pasta, Action<CardItem> novaSessao, Action<CardItem> trocarChange, Action<CardItem> remover)
    {
        Pasta = pasta;
        NovaSessaoCommand = new RelayCommand(() => novaSessao(this));
        TrocarChangeCommand = new RelayCommand(() => trocarChange(this));
        RemoverRepositorioCommand = new RelayCommand(() => remover(this));
    }

    public void Atualizar(CardNaVisao card, Demanda demanda, Func<string, SessaoItem> criarSessao,
        Func<string, LancamentoItem> criarLancamento)
    {
        DemandaId = demanda.Id;
        PodeNovaSessao = !demanda.Arquivada;
        Vazio = card.Sessoes.Count == 0 && card.Lancamentos.Count == 0;
        var repo = demanda.Repositorios.FirstOrDefault(r => string.Equals(r.Pasta, card.Pasta, StringComparison.OrdinalIgnoreCase));
        Change = repo?.OpenspecChange is null ? "" : TextoExibicao.Limpar(repo.OpenspecChange);
        TemChange = Change.Length > 0;
        if (!TemChange) AplicarProgresso(null);
        SincronizacaoColecao.Sincronizar(Sessoes, card.Sessoes,
            s => s.SessionId, s => s.Registrada.SessionId,
            s => criarSessao(s.Registrada.SessionId),
            (item, dado) => item.Atualizar(dado, emSessoes: false));
        SincronizacaoColecao.Sincronizar(Lancamentos, card.Lancamentos,
            l => l.Id, l => l.Lancamento.Id,
            l => criarLancamento(l.Lancamento.Id),
            (item, dado) => item.Atualizar(dado, card.Pasta, emSessoes: false));
    }

    /// <summary>Mostra o progresso da change deste repositório. Texto vem do Core, exibido como texto simples.</summary>
    public void AplicarProgresso(ProgressoRepositorio? p)
    {
        ProgressoTexto = p?.Texto ?? "";
        TemProgresso = ProgressoTexto.Length > 0;
        TemBarra = p?.Estado == EstadoRepositorio.Lido;
        ProgressoFracao = p?.Fracao ?? 0;
    }
}

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
