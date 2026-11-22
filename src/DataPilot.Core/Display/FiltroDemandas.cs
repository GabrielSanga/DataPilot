using System.Globalization;
using System.Text;
using DataPilot.Core.Models;

namespace DataPilot.Core.Display;

/// <summary>Filtro da lista de demandas, como função pura.</summary>
public static class FiltroDemandas
{
    /// <summary>Minúsculas e sem acentos, para comparar sem diferenciar maiúsculas nem acentos.</summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        var sb = new StringBuilder(texto.Length);
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// O trabalho casa quando o texto aparece no título, no nome de um repositório ou na change de um repositório
    /// (o identificador não entra). Texto vazio (ou só espaços) casa com tudo.
    /// </summary>
    public static bool Casa(Demanda demanda, string? filtro)
    {
        var f = Normalizar(filtro?.Trim());
        if (f.Length == 0) return true;
        return Contem(demanda.Titulo, f)
            || demanda.Repositorios.Any(r => Contem(r.Pasta, f) || Contem(r.OpenspecChange, f));
    }

    private static bool Contem(string? texto, string filtroNormalizado)
        => Normalizar(texto).Contains(filtroNormalizado, StringComparison.Ordinal);
}
