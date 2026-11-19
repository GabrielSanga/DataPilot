namespace DataPilot.Core.Models;

public enum Agente { Claude, Codex }

/// <summary>Status como gravado pelo hook no arquivo.</summary>
public enum StatusGravado { Ativa, Executando, Aguardando, Atencao, Encerrada }

/// <summary>Status exibido, na ordem de urgência (menor valor = mais urgente).</summary>
public enum StatusExibido
{
    PrecisaAtencao = 0,
    Aguardando = 1,
    Executando = 2,
    Ativa = 3,
    Inativa = 4,
    Encerrada = 5,
    /// <summary>Sessão registrada sem arquivo de status. Último na ordem e fora do resumo do cabeçalho.</summary>
    SemStatus = 6,
}

/// <summary>
/// Sessão lida de um arquivo de status. Os valores servem para exibição e, junto com o agente e o nome da pasta,
/// para comparar com os lançamentos pendentes. <see cref="ArquivoCriadoEm"/> vem do sistema de arquivos
/// (não do conteúdo), é a data da primeira gravação do hook e é nulo quando a sessão não foi lida de um arquivo.
/// </summary>
public sealed record Sessao(
    Agente Agente,
    string SessionId,
    string Cwd,
    StatusGravado Status,
    DateTimeOffset Atualizado,
    DateTimeOffset? ArquivoCriadoEm = null);

/// <summary>Prazos e limites do painel.</summary>
public static class Constantes
{
    public const string PastaStatusPadrao = @"C:\ProgramData\Painel\sessoes";
    /// <summary>Raiz dos repositórios do usuário <c>ia</c>, a mesma do <c>abrir.ps1</c>.</summary>
    public const string RaizFontesPadrao = @"C:\Fontes";
    public const string ExecutavelTerminal = "wt.exe";
    public const string JanelaTerminal = "painel";
    public const string DestinoSsh = "ia@localhost";
    public const string ScriptAbrir = @"C:\Painel\abrir.ps1";
    public static readonly TimeSpan InatividadeAtiva = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan InatividadeEspera = TimeSpan.FromHours(8);
    public static readonly TimeSpan IdadeSessaoAntiga = TimeSpan.FromDays(3);
    public static readonly TimeSpan IntervaloReleitura = TimeSpan.FromSeconds(5);
    public const int TamanhoMaximoArquivo = 16 * 1024;
    public const int MaximoArquivos = 500;
    public const int MaximoTexto = 260;
}
