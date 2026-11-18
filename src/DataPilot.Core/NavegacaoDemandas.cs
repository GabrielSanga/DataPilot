using System.Globalization;
using System.Text;

namespace DataPilot.Core;

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
    /// A demanda casa quando o texto aparece no identificador, no título, na change do OpenSpec ou no nome de um
    /// repositório. Texto vazio (ou só espaços) casa com tudo.
    /// </summary>
    public static bool Casa(Demanda demanda, string? filtro)
    {
        var f = Normalizar(filtro?.Trim());
        if (f.Length == 0) return true;
        return Contem(demanda.Id, f)
            || Contem(demanda.Titulo, f)
            || Contem(demanda.OpenspecChange, f)
            || demanda.Repositorios.Any(r => Contem(r.Pasta, f));
    }

    private static bool Contem(string? texto, string filtroNormalizado)
        => Normalizar(texto).Contains(filtroNormalizado, StringComparison.Ordinal);

    /// <summary>
    /// Mantém, na ordem original, os itens cuja demanda casa com o filtro. Um item sem demanda (a entrada
    /// "Não vinculadas") é sempre mantido.
    /// </summary>
    public static List<T> Filtrar<T>(IEnumerable<T> itens, string? filtro, Func<T, Demanda?> demanda)
        => itens.Where(i => demanda(i) is not { } d || Casa(d, filtro)).ToList();
}

/// <summary>Para onde "Ir para a próxima" leva: uma demanda ou "Não vinculadas" (<see cref="DemandaId"/> nulo).</summary>
public sealed record DestinoProxima(string? DemandaId)
{
    public bool EhNaoVinculadas => DemandaId is null;
}

/// <summary>Escolha da próxima demanda com sessão no status destacado no topo, como função pura.</summary>
public static class ProximaSessao
{
    /// <summary>
    /// Percorre as demandas na ordem da lista, depois da selecionada e voltando ao início, e por fim "Não vinculadas"
    /// (que fica no fim da lista), procurando sessão no status destacado. Devolve null se não houver status
    /// destacado ou nenhuma sessão nele. Sem seleção, a busca começa no início da lista.
    /// </summary>
    /// <param name="selecionadaId">Identificador da demanda selecionada, ou null.</param>
    /// <param name="naoVinculadasSelecionada">"Não vinculadas" está selecionada.</param>
    public static DestinoProxima? Escolher(Visao visao, string? selecionadaId, bool naoVinculadasSelecionada)
    {
        if (visao.ResumoCabecalho.StatusDestacado is not { } status) return null;

        // Posição na lista: demandas 0..n-1 e "Não vinculadas" em n.
        var candidatos = new List<(DestinoProxima Destino, bool Tem)>();
        foreach (var d in visao.Demandas)
        {
            var tem = d.Cards.SelectMany(c => c.Sessoes).Any(s => s.Status == status);
            candidatos.Add((new DestinoProxima(d.Demanda.Id), tem));
        }
        candidatos.Add((new DestinoProxima(null), visao.NaoVinculadas.Any(s => s.Status == status)));

        var atual = -1;
        if (naoVinculadasSelecionada) atual = candidatos.Count - 1;
        else if (selecionadaId is not null)
            atual = candidatos.FindIndex(c => c.Destino.DemandaId is { } id
                && string.Equals(id, selecionadaId, StringComparison.OrdinalIgnoreCase));

        // Depois da selecionada, dando a volta; a própria selecionada é a última opção.
        for (var passo = 1; passo <= candidatos.Count; passo++)
        {
            var c = candidatos[(atual + passo + candidatos.Count) % candidatos.Count];
            if (c.Tem) return c.Destino;
        }
        return null;
    }
}
