using DataPilot.Core.Arquivos;
using DataPilot.Core.Validation;

namespace DataPilot.Core.OpenSpec;

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
            if (LeituraSegura.Tipo(caminho) != LeituraSegura.TipoEntrada.Normal) return [];

        try
        {
            return LeituraSegura.Subpastas(changes)
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
