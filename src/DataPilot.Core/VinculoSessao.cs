namespace DataPilot.Core;

/// <summary>
/// Pedido de vínculo de uma sessão que o painel não abriu. É o único ponto em que um valor lido de arquivo
/// de status passa ao arquivo de dados: só o agente e o identificador (GUID canônico) cruzam. Não há como
/// criar um pedido sem passar por <see cref="Criar"/>, nem como carregar nele a pasta, o status ou a data.
/// </summary>
public sealed class PedidoVinculo
{
    public Agente Agente { get; }
    public string SessionId { get; }

    private PedidoVinculo(Agente agente, string sessionId)
    {
        Agente = agente;
        SessionId = sessionId;
    }

    /// <summary>Cria o pedido a partir de uma sessão lida; null se o identificador não for um GUID canônico.</summary>
    public static PedidoVinculo? Criar(Sessao lida)
        => ValidadorEntrada.IdentificadorValido(lida.SessionId) ? new PedidoVinculo(lida.Agente, lida.SessionId) : null;
}

/// <summary>Resultado da pergunta "esta sessão pode ser vinculada?", com o motivo quando não pode.</summary>
public sealed record PodeVincular(bool Sim, string? Motivo)
{
    public static PodeVincular Pode { get; } = new(true, null);
}

/// <summary>Regras de tela do vínculo, como funções puras.</summary>
public static class RegrasVinculo
{
    public const string MotivoIdentificadorInvalido =
        "O identificador desta sessão não é um GUID; o painel não conseguiria retomá-la.";

    public static PodeVincular PodeVincularSessao(Sessao lida)
        => ValidadorEntrada.IdentificadorValido(lida.SessionId)
            ? PodeVincular.Pode
            : new PodeVincular(false, MotivoIdentificadorInvalido);

    /// <summary>
    /// A pasta de trabalho da sessão tem como último segmento o nome do repositório (sem diferenciar maiúsculas).
    /// Só decide a pré-seleção do diálogo de vínculo e se há aviso; nunca é gravada.
    /// </summary>
    public static bool PastaCoincide(Sessao lida, string repositorio)
        => string.Equals(TextoExibicao.NomePasta(lida.Cwd), repositorio, StringComparison.OrdinalIgnoreCase);

    /// <summary>Avisar antes de vincular quando a pasta informada pela sessão não coincide com o repositório escolhido.</summary>
    public static bool AvisarPastaDivergente(Sessao lida, string repositorio) => !PastaCoincide(lida, repositorio);
}
