using System.Text;

namespace DataPilot.Core;

/// <summary>Limpeza de texto vindo dos arquivos e extração do nome da pasta.</summary>
public static class TextoExibicao
{
    public static string Limpar(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        var sb = new StringBuilder(Math.Min(texto.Length, Constantes.MaximoTexto + 1));
        foreach (var rune in texto.EnumerateRunes())
        {
            if (Rune.IsControl(rune) || IsFormatoOuSeparador(rune)) continue;
            sb.Append(rune.ToString());
        }
        var limpo = sb.ToString();
        if (limpo.Length <= Constantes.MaximoTexto) return limpo;
        var corte = Constantes.MaximoTexto;
        if (char.IsHighSurrogate(limpo[corte - 1])) corte--;
        return limpo[..corte] + "…";
    }

    private static bool IsFormatoOuSeparador(Rune r)
    {
        var cat = Rune.GetUnicodeCategory(r);
        return cat is System.Globalization.UnicodeCategory.Format
            or System.Globalization.UnicodeCategory.LineSeparator
            or System.Globalization.UnicodeCategory.ParagraphSeparator;
    }

    /// <summary>Último segmento do caminho, já limpo. Pasta vazia ou raiz resulta no próprio texto limpo.</summary>
    public static string NomePasta(string? cwd)
    {
        var limpo = Limpar(cwd);
        var aparado = limpo.TrimEnd('\\', '/');
        if (aparado.Length == 0) return limpo;
        var i = aparado.LastIndexOfAny(['\\', '/']);
        return i < 0 ? aparado : aparado[(i + 1)..];
    }
}
