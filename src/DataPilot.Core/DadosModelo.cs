namespace DataPilot.Core;

/// <summary>Sessão registrada no arquivo de dados do painel. Nenhum campo vem de arquivo de status.</summary>
public sealed record SessaoRegistrada(
    Agente Agente,
    string SessionId,
    string Pasta,
    string? Rotulo,
    DateTimeOffset CriadaEm);

/// <summary>Repositório cadastrado no arquivo de dados do painel, com as sessões registradas nele.</summary>
public sealed record RepositorioCadastrado(string Pasta, IReadOnlyList<SessaoRegistrada> Sessoes);

/// <summary>Resultado da leitura do arquivo de dados. Indisponível quando o arquivo existe mas não é legível.</summary>
public sealed record EstadoDados(bool Disponivel, IReadOnlyList<RepositorioCadastrado> Repositorios, string? Erro)
{
    public static EstadoDados Vazio { get; } = new(true, [], null);
}

public sealed record ResultadoOperacao(bool Ok, string? Mensagem = null)
{
    public static ResultadoOperacao Sucesso { get; } = new(true);
    public static ResultadoOperacao Falha(string mensagem) => new(false, mensagem);
}
