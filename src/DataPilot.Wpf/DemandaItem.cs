using System.Collections.ObjectModel;
using System.Windows.Media;
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
    [ObservableProperty] private string _tempo = "";

    public IRelayCommand AbrirCommand { get; }
    public IRelayCommand CancelarCommand { get; }

    public LancamentoItem(string id, Action<LancamentoItem> abrir, Action<LancamentoItem> cancelar)
    {
        Id = id;
        AbrirCommand = new RelayCommand(() => abrir(this));
        CancelarCommand = new RelayCommand(() => cancelar(this));
    }

    public void Atualizar(LancamentoNaVisao v)
    {
        var l = v.Lancamento;
        Agente = l.Agente == DataPilot.Core.Agente.Claude ? "Claude" : "Codex";
        Rotulo = l.Rotulo is null ? "" : TextoExibicao.Limpar(l.Rotulo);
        Tempo = v.Tempo.Length == 0 ? "—" : v.Tempo;
    }
}

/// <summary>Card de um repositório dentro de uma demanda. Chave: a pasta (única dentro da demanda).</summary>
public sealed partial class CardItem : ObservableObject
{
    public string Pasta { get; }
    public string DemandaId { get; private set; } = "";
    public ObservableCollection<SessaoItem> Sessoes { get; } = [];
    public ObservableCollection<LancamentoItem> Lancamentos { get; } = [];

    [ObservableProperty] private bool _vazio;
    /// <summary>Progresso da change neste repositório ("4 de 6"); vazio quando não há o que mostrar.</summary>
    [ObservableProperty] private string _progresso = "";

    [ObservableProperty] private bool _podeNovaSessao;
    public IRelayCommand NovaSessaoCommand { get; }
    public IRelayCommand RemoverRepositorioCommand { get; }

    public CardItem(string pasta, Action<CardItem> novaSessao, Action<CardItem> remover)
    {
        Pasta = pasta;
        NovaSessaoCommand = new RelayCommand(() => novaSessao(this));
        RemoverRepositorioCommand = new RelayCommand(() => remover(this));
    }

    public void Atualizar(CardNaVisao card, Demanda demanda, Func<string, SessaoItem> criarSessao,
        Func<string, LancamentoItem> criarLancamento)
    {
        DemandaId = demanda.Id;
        PodeNovaSessao = !demanda.Arquivada;
        Vazio = card.Sessoes.Count == 0 && card.Lancamentos.Count == 0;
        SincronizacaoColecao.Sincronizar(Sessoes, card.Sessoes,
            s => s.SessionId, s => s.Registrada.SessionId,
            s => criarSessao(s.Registrada.SessionId),
            (item, dado) => item.Atualizar(dado));
        SincronizacaoColecao.Sincronizar(Lancamentos, card.Lancamentos,
            l => l.Id, l => l.Lancamento.Id,
            l => criarLancamento(l.Lancamento.Id),
            (item, dado) => item.Atualizar(dado));
    }
}

/// <summary>
/// Entrada da lista de demandas, também usada para "Não vinculadas" (<see cref="EhNaoVinculadas"/>).
/// Chave: o <c>id</c> da demanda; a da entrada especial é fixa.
/// </summary>
public sealed partial class DemandaItem : ObservableObject
{
    public const string ChaveNaoVinculadas = "\u0001nao-vinculadas";

    public string Chave { get; }
    public bool EhNaoVinculadas { get; }
    public Demanda? Demanda { get; private set; }
    public bool TemSessaoEmAndamento { get; private set; }

    public ObservableCollection<CardItem> Cards { get; } = [];
    public ObservableCollection<SessaoItem> NaoVinculadas { get; } = [];

    [ObservableProperty] private string _cabecalho = "";
    [ObservableProperty] private string _openspec = "";
    [ObservableProperty] private string _resumo = "";
    /// <summary>Texto da barra de progresso da change ("7 de 12 tarefas") ou o aviso que a substitui.</summary>
    [ObservableProperty] private string _progressoTexto = "";
    [ObservableProperty] private bool _temProgresso;
    [ObservableProperty] private bool _temBarra;
    [ObservableProperty] private double _progressoFracao;
    [ObservableProperty] private Brush _corStatus = Cores.Cinza;
    [ObservableProperty] private bool _arquivada;
    [ObservableProperty] private bool _semRepositorios;
    [ObservableProperty] private bool _semNaoVinculadas;
    [ObservableProperty] private bool _podeEditar;
    [ObservableProperty] private bool _podeArquivar;
    [ObservableProperty] private bool _podeDesarquivar;
    [ObservableProperty] private bool _podeVincular;

    public IRelayCommand EditarCommand { get; }
    public IRelayCommand ArquivarCommand { get; }
    public IRelayCommand DesarquivarCommand { get; }
    public IRelayCommand VincularCommand { get; }

    public DemandaItem(string chave, bool naoVinculadas,
        Action<DemandaItem> editar, Action<DemandaItem> arquivar, Action<DemandaItem> desarquivar, Action<DemandaItem> vincular)
    {
        Chave = chave;
        EhNaoVinculadas = naoVinculadas;
        EditarCommand = new RelayCommand(() => editar(this));
        ArquivarCommand = new RelayCommand(() => arquivar(this));
        DesarquivarCommand = new RelayCommand(() => desarquivar(this));
        VincularCommand = new RelayCommand(() => vincular(this));
    }

    public void Atualizar(DemandaNaVisao v, Func<string, CardItem> criarCard, Func<string, SessaoItem> criarSessao,
        Func<string, LancamentoItem> criarLancamento)
    {
        var d = v.Demanda;
        Demanda = d;
        TemSessaoEmAndamento = v.TemSessaoEmAndamento;
        Cabecalho = $"{TextoExibicao.Limpar(d.Id)} · {TextoExibicao.Limpar(d.Titulo)}";
        Openspec = d.OpenspecChange is null ? "" : $"OpenSpec: {TextoExibicao.Limpar(d.OpenspecChange)}";
        Resumo = v.Resumo + (d.Arquivada ? " · arquivada" : "");
        CorStatus = v.MaisUrgente is { } s ? Cores.Fundo(s) : Cores.Cinza;
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

    /// <summary>Mostra o progresso da change na área da demanda e em cada card. Texto vem do Core, exibido como texto simples.</summary>
    public void AplicarProgresso(ProgressoDemanda p)
    {
        ProgressoTexto = p.Texto;
        TemProgresso = p.Texto.Length > 0;
        TemBarra = p.TemSoma;
        ProgressoFracao = p.Fracao;
        foreach (var card in Cards)
            card.Progresso = p.Repositorios.FirstOrDefault(r => r.Pasta == card.Pasta)?.Texto ?? "";
    }

    public void AtualizarNaoVinculadas(IReadOnlyList<SessaoExibida> sessoes, Func<string, SessaoItem> criarSessao)
    {
        Cabecalho = "Não vinculadas";
        Resumo = $"{sessoes.Count} {(sessoes.Count == 1 ? "sessão" : "sessões")}";
        SemNaoVinculadas = sessoes.Count == 0;
        SincronizacaoColecao.Sincronizar(NaoVinculadas, sessoes,
            s => s.SessionId, s => s.Sessao.SessionId,
            s => criarSessao(s.Sessao.SessionId),
            (item, dado) => item.AtualizarNaoVinculada(dado));
    }
}
