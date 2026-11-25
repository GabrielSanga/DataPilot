using static DataPilot.Core.Arquivos.LeituraSegura;
using System.Globalization;
using DataPilot.Core.Validation;

namespace DataPilot.Core.OpenSpec;

public enum EstadoLocalizacao
{
    /// <summary>A change não existe no repositório (nem ativa, nem arquivada).</summary>
    Nenhum,
    Encontrado,
    /// <summary>Entrada inválida ou trecho do caminho que é atalho ou junção.</summary>
    Recusado,
}

/// <summary>Onde está o <c>tasks.md</c> de uma change num repositório. <see cref="Trechos"/> é o caminho a conferir.</summary>
public sealed record LocalTasks(EstadoLocalizacao Estado, string? Caminho, bool Arquivada, IReadOnlyList<string> Trechos)
{
    public static readonly LocalTasks SemChange = new(EstadoLocalizacao.Nenhum, null, false, []);
    public static readonly LocalTasks Negado = new(EstadoLocalizacao.Recusado, null, false, []);
}

/// <summary>
/// Localiza o <c>tasks.md</c> da change: a ativa (<c>openspec\changes\&lt;change&gt;</c>) tem prioridade; senão, a
/// arquivada de data mais recente (<c>openspec\changes\archive\AAAA-MM-DD-&lt;change&gt;</c>). O caminho é montado só
/// com a raiz recebida e com <c>pasta</c> e <c>change</c> revalidados.
/// </summary>
public static class LocalizadorTasks
{
    public const string NomeArquivo = "tasks.md";

    public static LocalTasks Localizar(string raiz, string pasta, string change)
    {
        if (!ValidadorEntrada.RepositorioValido(pasta) || !ValidadorEntrada.OpenspecChangeValido(change)) return LocalTasks.Negado;

        var repo = Path.Combine(raiz, pasta);
        var inicio = Tipo(repo);
        if (inicio == TipoEntrada.Inexistente) return LocalTasks.SemChange;
        if (inicio == TipoEntrada.Atalho) return LocalTasks.Negado;

        var changes = Path.Combine(repo, "openspec", "changes");
        var ativa = Path.Combine(changes, change);
        switch (Tipo(ativa))
        {
            case TipoEntrada.Normal:
            case TipoEntrada.Atalho:
                return Encontrado(raiz, [pasta, "openspec", "changes", change, NomeArquivo], false);
        }

        var tipoOpenspec = TiposAteChanges(repo);
        if (tipoOpenspec == TipoEntrada.Atalho) return LocalTasks.Negado;

        var arquivo = Path.Combine(changes, "archive");
        var tipoArquivo = Tipo(arquivo);
        if (tipoArquivo == TipoEntrada.Inexistente) return LocalTasks.SemChange;
        if (tipoArquivo == TipoEntrada.Atalho) return LocalTasks.Negado;

        string? melhor = null;
        DateTime melhorData = default;
        try
        {
            foreach (var nome in Subpastas(arquivo))
            {
                if (!TentarData(nome, change, out var data)) continue;
                if (melhor is null || data > melhorData || (data == melhorData && string.CompareOrdinal(nome, melhor) > 0))
                {
                    melhor = nome;
                    melhorData = data;
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return LocalTasks.Negado;
        }
        return melhor is null
            ? LocalTasks.SemChange
            : Encontrado(raiz, [pasta, "openspec", "changes", "archive", melhor, NomeArquivo], true);
    }

    private static LocalTasks Encontrado(string raiz, string[] trechos, bool arquivada)
        => new(EstadoLocalizacao.Encontrado, Path.Combine([raiz, .. trechos]), arquivada, trechos);

    /// <summary>Verifica <c>openspec</c> e <c>changes</c>: um atalho em qualquer um recusa.</summary>
    private static TipoEntrada TiposAteChanges(string repo)
    {
        foreach (var parte in new[] { "openspec", Path.Combine("openspec", "changes") })
            if (Tipo(Path.Combine(repo, parte)) == TipoEntrada.Atalho) return TipoEntrada.Atalho;
        return TipoEntrada.Normal;
    }

    private static bool TentarData(string nome, string change, out DateTime data)
    {
        data = default;
        var sufixo = "-" + change;
        // AAAA-MM-DD + "-" + change, comparando como texto.
        if (nome.Length != 10 + sufixo.Length || !nome.EndsWith(sufixo, StringComparison.Ordinal)) return false;
        var parte = nome[..10];
        foreach (var i in new[] { 0, 1, 2, 3, 5, 6, 8, 9 })
            if (parte[i] is < '0' or > '9') return false;
        return DateTime.TryParseExact(parte, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out data);
    }
}
