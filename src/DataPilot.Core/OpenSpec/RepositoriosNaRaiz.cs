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
            return ResultadoPasta.Recusada("Nome de pasta inválido: use apenas letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.");
        return ResultadoPasta.Aceita(nome);
    }
}

/// <summary>
/// Lista as changes ativas de um repositório (pastas de <c>openspec\changes</c>, sem <c>archive</c>). A pasta é do
/// usuário <c>ia</c>, então só entram pastas comuns (nem atalho nem junção, conferindo também <c>openspec</c> e
/// <c>changes</c>), de nome válido, até 200, em ordem alfabética. Os nomes só são exibidos; viram dado quando o
/// usuário escolhe um, e passam pela validação antes de gravar.
/// </summary>
public static class ChangesDoRepositorio
{
    public const int Maximo = 200;

    public static IReadOnlyList<string> Listar(string raiz, string pasta)
    {
        if (!ValidadorEntrada.PastaValida(pasta)) return [];
        var repo = Path.Combine(raiz, pasta);
        var changes = Path.Combine(repo, "openspec", "changes");
        foreach (var caminho in new[] { repo, Path.Combine(repo, "openspec"), changes })
            if (LocalizadorTasks.Tipo(caminho) != LocalizadorTasks.TipoEntrada.Normal) return [];

        try
        {
            return new DirectoryInfo(changes).EnumerateDirectories()
                .Where(d => (d.Attributes & FileAttributes.ReparsePoint) == 0)
                .Select(d => d.Name)
                .Where(n => !string.Equals(n, "archive", StringComparison.OrdinalIgnoreCase)
                            && ValidadorEntrada.OpenspecChangeValido(n))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ThenBy(n => n, StringComparer.Ordinal)
                .Take(Maximo)
                .ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
