namespace DataPilot.Core.Models;

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
