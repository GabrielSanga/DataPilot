using System.Text.RegularExpressions;

namespace DataPilot.Core.OpenSpec;

/// <summary>Tarefas concluídas e total de um <c>tasks.md</c>.</summary>
public sealed record ContagemTarefas(int Concluidas, int Total);

/// <summary>Conta as tarefas de um texto de <c>tasks.md</c>. O resultado são só dois números.</summary>
public static partial class ContadorTarefas
{
    // Mesma regra do PRD e do CLI do OpenSpec: caixa no início da linha, marcada com x ou X.
    [GeneratedRegex(@"\A\s*-\s\[( |x|X)\]")]
    private static partial Regex PadraoTarefa();

    public static ContagemTarefas Contar(string texto)
    {
        int concluidas = 0, total = 0;
        foreach (var linha in texto.Split('\n'))
        {
            var m = PadraoTarefa().Match(linha);
            if (!m.Success) continue;
            total++;
            if (m.Groups[1].Value != " ") concluidas++;
        }
        return new ContagemTarefas(concluidas, total);
    }
}
