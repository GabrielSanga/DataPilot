using DataPilot.Core.Arquivos;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Core.OpenSpec;

/// <summary>Resultado da validação da pasta escolhida na escolha de pasta do Windows.</summary>
public sealed record ResultadoPasta(bool Ok, string? Pasta, string? Erro)
{
    public static ResultadoPasta Aceita(string pasta) => new(true, pasta, null);
    public static ResultadoPasta Recusada(string erro) => new(false, null, erro);
}

/// <summary>
/// Valida a pasta escolhida para um repositório. Uma pasta direta da raiz dos fontes, com nome que siga a regra de
/// pasta, devolve só o nome (como sempre foi gravado); outra pasta dentro dos <see cref="LocaisContainer"/> devolve o
/// caminho completo; as demais são recusadas.
/// </summary>
public static class EscolhaDePasta
{
    private static readonly char[] Separadores = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public const string MensagemAtalho = "A pasta escolhida é um atalho ou junção, ou não pode ser lida; escolha a pasta de destino.";
    public const string MensagemInexistente = "A pasta escolhida não existe.";

    public static ResultadoPasta Validar(string raiz, string? caminhoEscolhido)
        => Validar(raiz, caminhoEscolhido, LeituraSegura.Tipo);

    /// <summary>A consulta ao disco entra por parâmetro para os testes não criarem pastas em <c>C:\Fontes</c>.</summary>
    internal static ResultadoPasta Validar(string raiz, string? caminhoEscolhido, Func<string, LeituraSegura.TipoEntrada> tipo)
    {
        if (string.IsNullOrWhiteSpace(caminhoEscolhido)) return ResultadoPasta.Recusada(ValidadorEntrada.MensagemRepositorio);
        string completo, raizCompleta;
        try
        {
            completo = Path.GetFullPath(caminhoEscolhido).TrimEnd(Separadores);
            raizCompleta = Path.GetFullPath(raiz).TrimEnd(Separadores);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return ResultadoPasta.Recusada(ValidadorEntrada.MensagemRepositorio);
        }

        var pai = Path.GetDirectoryName(completo)?.TrimEnd(Separadores);
        var nome = Path.GetFileName(completo);
        if (pai is not null && string.Equals(pai, raizCompleta, StringComparison.OrdinalIgnoreCase) && ValidadorEntrada.PastaValida(nome))
            return ResultadoPasta.Aceita(nome);

        if (LocaisContainer.Traduzir(completo) is null) return ResultadoPasta.Recusada(LocaisContainer.MensagemFora);
        if (!ValidadorEntrada.CaminhoValido(completo)) return ResultadoPasta.Recusada(ValidadorEntrada.MensagemRepositorio);
        // O abrir.sh recusa atalho e junção; recusar aqui evita gravar um repositório que nunca abre.
        return tipo(completo) switch
        {
            LeituraSegura.TipoEntrada.Normal => ResultadoPasta.Aceita(completo),
            LeituraSegura.TipoEntrada.Inexistente => ResultadoPasta.Recusada(MensagemInexistente),
            _ => ResultadoPasta.Recusada(MensagemAtalho),
        };
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
