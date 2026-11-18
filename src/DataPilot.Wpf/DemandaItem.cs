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
    [ObservableProperty] private string _textoPrincipal = "";
    [ObservableProperty] private bool _principalEhVazio;
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
        PrincipalEhVazio = Rotulo.Length == 0;
        TextoPrincipal = PrincipalEhVazio ? "sem rótulo" : Rotulo;
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

    /// <summary>Identificador e título juntos, para os textos dos diálogos.</summary>
    [ObservableProperty] private string _cabecalho = "";
    /// <summary>Identificador da demanda (em Bahnschrift na tela), ou "Não vinculadas".</summary>
    [ObservableProperty] private string _identificador = "";
    [ObservableProperty] private string _titulo = "";
    /// <summary>Nome da change do OpenSpec; vazio quando a demanda não tem.</summary>
    [ObservableProperty] private string _openspec = "";
    [ObservableProperty] private bool _temOpenspec;
    /// <summary>Contagens em frase, como "2 repositórios, 3 sessões".</summary>
    [ObservableProperty] private string _resumo = "";
    /// <summary>Nome do status mais urgente, só quando ele acende (precisa de atenção ou aguardando você).</summary>
    [ObservableProperty] private string _destaqueTexto = "";
    [ObservableProperty] private bool _temDestaque;
    /// <summary>Texto da barra de progresso da change ("7 de 12 tarefas") ou o aviso que a substitui.</summary>
    [ObservableProperty] private string _progressoTexto = "";
    [ObservableProperty] private bool _temProgresso;
    [ObservableProperty] private bool _temBarra;
    [ObservableProperty] private double _progressoFracao;
    [ObservableProperty] private NivelSinal _nivel;
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
        Identificador = TextoExibicao.Limpar(d.Id);
        Titulo = TextoExibicao.Limpar(d.Titulo);
        Cabecalho = $"{Identificador} — {Titulo}";
        Openspec = d.OpenspecChange is null ? "" : TextoExibicao.Limpar(d.OpenspecChange);
        TemOpenspec = Openspec.Length > 0;
        Resumo = v.Resumo + (d.Arquivada ? ", arquivada" : "");
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

    /// <summary>Faixa e nome do status só quando o mais urgente é um dos dois que precisam do usuário.</summary>
    private void AplicarSinal(StatusExibido? maisUrgente)
    {
        Nivel = Sinal.De(maisUrgente);
        TemDestaque = Sinal.Acende(maisUrgente);
        DestaqueTexto = TemDestaque ? RegrasExibicao.Texto(maisUrgente!.Value) : "";
    }

    public void AtualizarNaoVinculadas(IReadOnlyList<SessaoExibida> sessoes, Func<string, SessaoItem> criarSessao)
    {
        Identificador = "Não vinculadas";
        Titulo = "";
        Cabecalho = "Não vinculadas";
        Resumo = sessoes.Count switch { 0 => "nenhuma sessão", 1 => "1 sessão", var n => $"{n} sessões" };
        AplicarSinal(sessoes.Count == 0 ? null : sessoes.Min(s => s.Status));
        SemNaoVinculadas = sessoes.Count == 0;
        SincronizacaoColecao.Sincronizar(NaoVinculadas, sessoes,
            s => s.SessionId, s => s.Sessao.SessionId,
            s => criarSessao(s.Sessao.SessionId),
            (item, dado) => item.AtualizarNaoVinculada(dado));
    }
}
