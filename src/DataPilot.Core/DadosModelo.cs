namespace DataPilot.Core;

/// <summary>
/// Sessão registrada no arquivo de dados do painel. Nenhum campo vem de arquivo de status.
/// <see cref="DemandaId"/> é a demanda em que ela está registrada (vem do arquivo de dados, já validado) e
/// <see cref="DemandaTitulo"/> o título dela, usado no título da aba ao retomar.
/// </summary>
public sealed record SessaoRegistrada(
    Agente Agente,
    string SessionId,
    string Pasta,
    string? Rotulo,
    DateTimeOffset CriadaEm,
    string DemandaId,
    string? DemandaTitulo = null);

/// <summary>
/// Sessão que o usuário excluiu do painel. Guarda só o agente, o identificador (regra do hook) e a data.
/// </summary>
public sealed record SessaoExcluida(Agente Agente, string SessionId, DateTimeOffset ExcluidaEm);

/// <summary>
/// Sessão nova do Codex aberta pelo painel, cujo identificador ainda não existe. Fica no repositório do card em que
/// o usuário clicou até uma sessão surgir na pasta de status e ser vinculada a ela. Nenhum campo vem de arquivo de status.
/// </summary>
public sealed record LancamentoPendente(string Id, Agente Agente, DateTimeOffset CriadoEm, string? Rotulo);

/// <summary>
/// Repositório vinculado a uma demanda, com as sessões registradas e os lançamentos pendentes nele.
/// <see cref="OpenspecChange"/> é a change efetiva: a do repositório ou, na falta dela, a da demanda gravada por
/// versão anterior.
/// </summary>
public sealed record RepositorioCadastrado(
    string Pasta, IReadOnlyList<SessaoRegistrada> Sessoes, IReadOnlyList<LancamentoPendente> Lancamentos,
    string? OpenspecChange = null)
{
    public RepositorioCadastrado(string pasta, IReadOnlyList<SessaoRegistrada> sessoes) : this(pasta, sessoes, []) { }
}

/// <summary>
/// Demanda do arquivo de dados: reúne os repositórios e as sessões de um mesmo trabalho. <see cref="OpenspecChange"/>
/// é o campo antigo, só lido (cada repositório tem a própria change) e nunca gravado pelo painel.
/// </summary>
public sealed record Demanda(
    string Id,
    string Titulo,
    string? OpenspecChange,
    DateTimeOffset? CriadaEm,
    bool Arquivada,
    IReadOnlyList<RepositorioCadastrado> Repositorios);

/// <summary>Resultado da leitura do arquivo de dados. Indisponível quando o arquivo existe mas não é legível.</summary>
public sealed record EstadoDados(bool Disponivel, IReadOnlyList<Demanda> Demandas, string? Erro)
{
    public static EstadoDados Vazio { get; } = new(true, [], null);

    /// <summary>Sessões que o usuário excluiu; o painel não as mostra nem as vincula.</summary>
    public IReadOnlyList<SessaoExcluida> Excluidas { get; init; } = [];
}

public sealed record ResultadoOperacao(bool Ok, string? Mensagem = null)
{
    /// <summary>Identificador gerado pela operação (a criação de um trabalho), quando houver.</summary>
    public string? Id { get; init; }

    public static ResultadoOperacao Sucesso { get; } = new(true);
    public static ResultadoOperacao Falha(string mensagem) => new(false, mensagem);
}

/// <summary>Restrições da demanda fixa "Sem demanda" (a entrada "Sessões") e das demandas arquivadas, em um só lugar.</summary>
public static class RegrasDemanda
{
    public static bool EhImplicita(string id)
        => string.Equals(id, DadosPainel.IdDemandaImplicita, StringComparison.OrdinalIgnoreCase);

    public static bool PodeEditar(Demanda d) => !EhImplicita(d.Id);
    public static bool PodeArquivar(Demanda d) => !EhImplicita(d.Id) && !d.Arquivada;
    public static bool PodeDesarquivar(Demanda d) => d.Arquivada;
    public static bool PodeVincularRepositorio(Demanda d) => !d.Arquivada;
    public static bool PodeAbrirNovaSessao(Demanda d) => !d.Arquivada && d.Repositorios.Count > 0;
}
