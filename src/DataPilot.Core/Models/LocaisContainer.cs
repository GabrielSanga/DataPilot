namespace DataPilot.Core.Models;

/// <summary>Pastas do Windows montadas no container (as do <c>docker/compose.yml</c>) e o caminho de cada uma lá dentro.</summary>
public static class LocaisContainer
{
    private static readonly (string Windows, string Container)[] Locais =
    [
        (Constantes.RaizFontesPadrao, "/workspace"),
        (Constantes.PastaOpenspec, "/openspec/ERP"),
    ];

    public const string MensagemFora = "O container só enxerga " + Constantes.RaizFontesPadrao + " e " + Constantes.PastaOpenspec
        + ". Use um repositório num desses locais.";

    /// <summary>
    /// Caminho no container de uma pasta do Windows já validada (completa e normalizada), ou null se ela está fora
    /// dos locais montados.
    /// </summary>
    public static string? Traduzir(string caminho)
    {
        foreach (var (windows, container) in Locais)
        {
            if (string.Equals(caminho, windows, StringComparison.OrdinalIgnoreCase)) return container;
            if (caminho.StartsWith(windows + @"\", StringComparison.OrdinalIgnoreCase))
                return container + caminho[windows.Length..].Replace('\\', '/');
        }
        return null;
    }
}
