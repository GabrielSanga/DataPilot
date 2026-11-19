using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;

namespace DataPilot.Core.Display;

/// <summary>
/// Sessão registrada como a tela a mostra. <see cref="Lida"/> é null quando não há arquivo de status
/// (status exibido "sem status"). Quem usa a ação "Retomar" só enxerga <see cref="Registrada"/>.
/// </summary>
public sealed record SessaoNaVisao(
    SessaoRegistrada Registrada,
    Sessao? Lida,
    StatusExibido Status,
    string Tempo);

/// <summary>
/// Lançamento pendente como a tela o mostra: uma sessão nova do Codex ainda sem a primeira mensagem. Não é uma
/// sessão: não tem status, identificador de sessão nem "Retomar". Tudo vem do arquivo de dados.
/// </summary>
public sealed record LancamentoNaVisao(LancamentoPendente Lancamento, string Tempo);

/// <summary>
/// Card de um repositório: sessões registradas e, depois delas, os lançamentos pendentes válidos (do mais recente
/// para o mais antigo). As sessões não registradas ficam na entrada "Sessões".
/// </summary>
public sealed record CardNaVisao(string Pasta, IReadOnlyList<SessaoNaVisao> Sessoes, IReadOnlyList<LancamentoNaVisao> Lancamentos)
{
    public CardNaVisao(string pasta, IReadOnlyList<SessaoNaVisao> sessoes) : this(pasta, sessoes, []) { }
}

/// <summary>Demanda com o resumo da lista: repositórios, sessões e status mais urgente (null se nenhuma sessão tem status).</summary>
public sealed record DemandaNaVisao(
    Demanda Demanda,
    IReadOnlyList<CardNaVisao> Cards,
    int QuantidadeRepositorios,
    int QuantidadeSessoes,
    StatusExibido? MaisUrgente,
    string Resumo)
{
    /// <summary>Alguma sessão exibida em status que pede confirmação antes de arquivar.</summary>
    public bool TemSessaoEmAndamento
        => Cards.SelectMany(c => c.Sessoes).Any(s => Correspondencia.ExigeConfirmacao(s.Status));
}

/// <summary>Lançamento pendente da entrada "Sessões", com o repositório em que foi aberto.</summary>
public sealed record LancamentoEmSessoes(string Pasta, LancamentoNaVisao Lancamento);

/// <summary>
/// Entrada "Sessões" (a demanda fixa <c>sem-demanda</c>): sessões registradas nela, com o repositório de cada uma
/// (<see cref="SessaoRegistrada.Pasta"/>), lançamentos pendentes e sessões lidas da pasta de status que não estão
/// registradas em nenhuma demanda nem foram excluídas.
/// </summary>
public sealed record EntradaSessoes(
    IReadOnlyList<SessaoNaVisao> Registradas,
    IReadOnlyList<LancamentoEmSessoes> Lancamentos,
    IReadOnlyList<SessaoExibida> NaoRegistradas)
{
    public static EntradaSessoes Vazia { get; } = new([], [], []);

    public int Quantidade => Registradas.Count + NaoRegistradas.Count;

    /// <summary>Algum status da entrada, para "Ir para a próxima" e para o resumo da lista.</summary>
    public bool TemStatus(StatusExibido status)
        => Registradas.Any(s => s.Status == status) || NaoRegistradas.Any(s => s.Status == status);

    /// <summary>Status mais urgente das sessões com status, ou null.</summary>
    public StatusExibido? MaisUrgente
    {
        get
        {
            var todos = Registradas.Where(s => s.Status != StatusExibido.SemStatus).Select(s => s.Status)
                .Concat(NaoRegistradas.Select(s => s.Status)).ToList();
            return todos.Count == 0 ? null : todos.Min();
        }
    }
}

/// <summary>Tela inteira: trabalhos ordenados, a entrada "Sessões" e o resumo do cabeçalho.</summary>
public sealed record Visao(
    IReadOnlyList<DemandaNaVisao> Demandas,
    EntradaSessoes Sessoes,
    ResumoCabecalho ResumoCabecalho);

/// <summary>Monta a visão agrupada por demanda como função pura; o relógio entra por <see cref="TimeProvider"/>.</summary>
public static class VisaoAgrupada
{
    public static Visao Montar(
        IReadOnlyList<Demanda> demandas,
        IEnumerable<Sessao> sessoesLidas,
        bool mostrarArquivadas,
        bool mostrarAntigas,
        TimeProvider relogio,
        IReadOnlyList<SessaoExcluida>? excluidas = null)
    {
        var excluidasSet = new HashSet<(Agente, string)>(
            (excluidas ?? []).Select(e => (e.Agente, e.SessionId.ToLowerInvariant())));

        var lidas = new Dictionary<(Agente, string), Sessao>();
        foreach (var s in sessoesLidas)
        {
            var chave = (s.Agente, s.SessionId.ToLowerInvariant());
            if (excluidasSet.Contains(chave)) continue;
            lidas.TryAdd(chave, s);
        }

        // Toda sessão registrada, de qualquer demanda (inclusive arquivada ou oculta), deixa de ser "não registrada".
        var registradas = new HashSet<(Agente, string)>();
        foreach (var d in demandas)
            foreach (var r in d.Repositorios)
                foreach (var s in r.Sessoes)
                    registradas.Add((s.Agente, s.SessionId.ToLowerInvariant()));

        var naoRegistradas = RegrasExibicao.Montar(
            lidas.Where(p => !registradas.Contains((p.Key.Item1, p.Key.Item2))).Select(p => p.Value),
            mostrarAntigas, relogio);

        var itens = new List<DemandaNaVisao>();
        var sessoes = EntradaSessoes.Vazia with { NaoRegistradas = naoRegistradas };
        foreach (var d in demandas)
        {
            if (RegrasDemanda.EhImplicita(d.Id))
            {
                sessoes = MontarSessoes(d, lidas, excluidasSet, naoRegistradas, relogio);
                continue;
            }
            if (d.Arquivada && !mostrarArquivadas) continue;
            itens.Add(MontarDemanda(d, lidas, excluidasSet, relogio));
        }
        var ordenadas = Ordenar(itens);

        // O cabeçalho conta as sessões dos trabalhos listados e as de "Sessões"; "sem status" não entra.
        var paraResumo = ordenadas
            .SelectMany(d => d.Cards).SelectMany(c => c.Sessoes)
            .Concat(sessoes.Registradas)
            .Where(s => s.Lida is not null)
            .Select(s => new SessaoExibida(s.Lida!, s.Status, s.Tempo))
            .Concat(naoRegistradas);

        return new Visao(ordenadas, sessoes, RegrasExibicao.Resumo(paraResumo));
    }

    private static EntradaSessoes MontarSessoes(Demanda d, Dictionary<(Agente, string), Sessao> lidas,
        HashSet<(Agente, string)> excluidas, IReadOnlyList<SessaoExibida> naoRegistradas, TimeProvider relogio)
    {
        var cards = MontarCards(d, lidas, excluidas, relogio);
        var registradas = cards.SelectMany(c => c.Sessoes).ToList();
        registradas.Sort(ComparaSessoes);
        var lancamentos = cards.SelectMany(c => c.Lancamentos.Select(l => new LancamentoEmSessoes(c.Pasta, l)))
            .OrderByDescending(x => x.Lancamento.Lancamento.CriadoEm)
            .ThenBy(x => x.Lancamento.Lancamento.Id, StringComparer.Ordinal)
            .ToList();
        return new EntradaSessoes(registradas, lancamentos, naoRegistradas);
    }

    private static List<CardNaVisao> MontarCards(Demanda d, Dictionary<(Agente, string), Sessao> lidas,
        HashSet<(Agente, string)> excluidas, TimeProvider relogio)
    {
        var cards = new List<CardNaVisao>();
        foreach (var repo in d.Repositorios)
        {
            var sessoes = new List<SessaoNaVisao>();
            foreach (var r in repo.Sessoes)
            {
                if (excluidas.Contains((r.Agente, r.SessionId.ToLowerInvariant()))) continue;
                if (lidas.TryGetValue((r.Agente, r.SessionId.ToLowerInvariant()), out var lida))
                    sessoes.Add(new SessaoNaVisao(r, lida, RegrasExibicao.StatusExibido(lida, relogio),
                        RegrasExibicao.TempoDecorrido(RegrasExibicao.Idade(lida, relogio))));
                else
                    sessoes.Add(new SessaoNaVisao(r, null, StatusExibido.SemStatus, ""));
            }
            sessoes.Sort(ComparaSessoes);
            cards.Add(new CardNaVisao(repo.Pasta, sessoes, MontarLancamentos(d, repo, relogio)));
        }
        return cards;
    }

    private static DemandaNaVisao MontarDemanda(Demanda d, Dictionary<(Agente, string), Sessao> lidas,
        HashSet<(Agente, string)> excluidas, TimeProvider relogio)
    {
        var cards = MontarCards(d, lidas, excluidas, relogio);

        var todas = cards.SelectMany(c => c.Sessoes).ToList();
        var comStatus = todas.Where(s => s.Status != StatusExibido.SemStatus).ToList();
        StatusExibido? urgente = comStatus.Count == 0 ? null : comStatus.Min(s => s.Status);
        return new DemandaNaVisao(d, cards, cards.Count, todas.Count, urgente, TextoResumo(cards.Count, todas.Count));
    }

    /// <summary>
    /// Lançamentos pendentes válidos do repositório, do mais recente para o mais antigo. Vencidos e os de demanda
    /// arquivada não aparecem, pela mesma regra que os impede de vincular. Ficam fora de todas as contagens.
    /// </summary>
    private static List<LancamentoNaVisao> MontarLancamentos(Demanda d, RepositorioCadastrado repo, TimeProvider relogio)
    {
        if (d.Arquivada) return [];
        var agora = relogio.GetUtcNow();
        return repo.Lancamentos
            .Where(l => agora - l.CriadoEm <= DadosPainel.ValidadeLancamento)
            .OrderByDescending(l => l.CriadoEm)
            .ThenBy(l => l.Id, StringComparer.Ordinal)
            .Select(l => new LancamentoNaVisao(l, RegrasExibicao.TempoDecorrido(agora - l.CriadoEm < TimeSpan.Zero ? TimeSpan.Zero : agora - l.CriadoEm)))
            .ToList();
    }

    /// <summary>"2 repositórios, 3 sessões"; zero vira "nenhum repositório" e "nenhuma sessão".</summary>
    public static string TextoResumo(int repositorios, int sessoes)
    {
        var r = repositorios switch { 0 => "nenhum repositório", 1 => "1 repositório", _ => $"{repositorios} repositórios" };
        var s = sessoes switch { 0 => "nenhuma sessão", 1 => "1 sessão", _ => $"{sessoes} sessões" };
        return $"{r}, {s}";
    }

    /// <summary>Status exibido; no mesmo status, atualização mais recente primeiro (sem status: registro mais recente).</summary>
    private static int ComparaSessoes(SessaoNaVisao a, SessaoNaVisao b)
    {
        var c = a.Status.CompareTo(b.Status);
        if (c != 0) return c;
        var da = a.Lida?.Atualizado ?? a.Registrada.CriadaEm;
        var db = b.Lida?.Atualizado ?? b.Registrada.CriadaEm;
        c = db.CompareTo(da);
        return c != 0 ? c : string.CompareOrdinal(a.Registrada.SessionId, b.Registrada.SessionId);
    }

    private static DateTimeOffset? Atualizacao(DemandaNaVisao d)
    {
        var datas = d.Cards.SelectMany(c => c.Sessoes).Where(s => s.Lida is not null).Select(s => s.Lida!.Atualizado).ToList();
        return datas.Count == 0 ? null : datas.Max();
    }

    /// <summary>Não arquivadas primeiro; depois status mais urgente, atualização mais recente; sem status por último. Estável.</summary>
    private static List<DemandaNaVisao> Ordenar(List<DemandaNaVisao> itens)
    {
        return itens
            .Select((d, i) => (d, i))
            .OrderBy(x => x.d.Demanda.Arquivada)
            .ThenBy(x => x.d.MaisUrgente is null)
            .ThenBy(x => x.d.MaisUrgente ?? StatusExibido.SemStatus)
            .ThenByDescending(x => Atualizacao(x.d) ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.i)
            .Select(x => x.d)
            .ToList();
    }
}
