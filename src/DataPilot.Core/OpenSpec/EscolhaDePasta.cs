using DataPilot.Core.Arquivos;
using DataPilot.Core.Validation;

namespace DataPilot.Core.OpenSpec;

/// <summary>Resultado da validação da pasta escolhida na escolha de pasta do Windows.</summary>
public sealed record ResultadoPasta(bool Ok, string? Pasta, string? Erro)
{
    public static ResultadoPasta Aceita(string pasta) => new(true, pasta, null);
    public static ResultadoPasta Recusada(string erro) => new(false, null, erro);
}

/// <summary>
/// Valida a pasta escolhida para um repositório: só uma pasta filha direta da raiz dos fontes, com nome que siga a
/// regra de pasta, é aceita (é o que o <c>abrir.ps1</c> abre). Só o nome é devolvido, e é só ele que se grava.
/// </summary>
public static class EscolhaDePasta
{
    private static readonly char[] Separadores = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static string MensagemForaDaRaiz(string raiz)
        => $"Só pastas diretas de {raiz} podem ser usadas.";

    public static ResultadoPasta Validar(string raiz, string? caminhoEscolhido)
    {
        if (string.IsNullOrWhiteSpace(caminhoEscolhido)) return ResultadoPasta.Recusada(MensagemForaDaRaiz(raiz));
        string completo, raizCompleta;
        try
        {
            completo = Path.GetFullPath(caminhoEscolhido).TrimEnd(Separadores);
            raizCompleta = Path.GetFullPath(raiz).TrimEnd(Separadores);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return ResultadoPasta.Recusada(MensagemForaDaRaiz(raiz));
        }

        var pai = Path.GetDirectoryName(completo)?.TrimEnd(Separadores);
        if (pai is null || !string.Equals(pai, raizCompleta, StringComparison.OrdinalIgnoreCase))
            return ResultadoPasta.Recusada(MensagemForaDaRaiz(raiz));

        var nome = Path.GetFileName(completo);
        if (!ValidadorEntrada.PastaValida(nome))
            return ResultadoPasta.Recusada(ValidadorEntrada.MensagemPasta);
        return ResultadoPasta.Aceita(nome);
    }

    /// <summary>Pastas diretas da raiz dos fontes com nome válido e que não são atalhos nem junções, em ordem.</summary>
    public static IReadOnlyList<string> PastasDaRaiz(string raiz)
    {
        try
        {
            return LeituraSegura.Subpastas(raiz)
                .Where(ValidadorEntrada.PastaValida)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
