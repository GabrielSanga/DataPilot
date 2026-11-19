namespace DataPilot.Core.Models;

#region "Enums"

    public enum Agente
    {
        Claude,
        Codex
    }

    public enum StatusGravado
    {
        Ativa,
        Executando,
        Aguardando,
        Atencao,
        Encerrada
    }

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

#endregion

public sealed record Sessao(Agente Agente, string SessionId, string Cwd, StatusGravado Status, DateTimeOffset Atualizado, DateTimeOffset? ArquivoCriadoEm = null);

public static class Constantes
{
    public const string PastaStatusPadrao = @"C:\ProgramData\Painel\sessoes";
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
