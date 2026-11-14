using System.Globalization;
using System.Text.RegularExpressions;

namespace DataPilot.Core;

/// <summary>
/// Regras de validação das entradas que podem chegar a um comando: pasta, identificador de sessão e rótulo.
/// É o único lugar onde elas estão escritas; cadastro, carga do arquivo de dados e montador usam este tipo.
/// </summary>
public static partial class ValidadorEntrada
{
    public const int TamanhoMaximoRotulo = 40;

    // As duas primeiras repetem as regras do abrir.ps1; \z evita aceitar uma quebra de linha final.
    [GeneratedRegex(@"\A[A-Za-z0-9_.-]+\z")]
    private static partial Regex PadraoPasta();

    [GeneratedRegex(@"\A[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}\z")]
    private static partial Regex PadraoGuid();

    /// <summary>Só <c>[A-Za-z0-9_.-]</c> e nunca formada apenas por pontos.</summary>
    public static bool PastaValida(string? pasta)
        => pasta is not null && PadraoPasta().IsMatch(pasta) && pasta.Any(c => c != '.');

    /// <summary>GUID na forma canônica 8-4-4-4-12.</summary>
    public static bool IdentificadorValido(string? id)
        => id is not null && PadraoGuid().IsMatch(id);

    /// <summary>
    /// De 1 a 40 caracteres, sem <c>;</c> (o Windows Terminal o trata como separador de comandos)
    /// e sem caracteres de controle ou separadores de linha.
    /// </summary>
    public static bool RotuloValido(string? rotulo)
    {
        if (string.IsNullOrEmpty(rotulo) || rotulo.Length > TamanhoMaximoRotulo) return false;
        foreach (var c in rotulo)
        {
            if (c == ';' || char.IsControl(c)) return false;
            var cat = char.GetUnicodeCategory(c);
            if (cat is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator) return false;
        }
        return true;
    }

    /// <summary>Texto do agente como usado no arquivo de dados e no comando.</summary>
    public static string NomeAgente(Agente agente) => agente == Agente.Claude ? "claude" : "codex";

    public static bool TentarAgente(string? texto, out Agente agente)
    {
        switch (texto)
        {
            case "claude": agente = Agente.Claude; return true;
            case "codex": agente = Agente.Codex; return true;
            default: agente = default; return false;
        }
    }
}
