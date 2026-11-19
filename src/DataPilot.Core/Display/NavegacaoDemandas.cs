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

    /// <summary>
    /// Mantém, na ordem original, os itens cuja demanda casa com o filtro. Um item sem demanda (a entrada "Sessões") é sempre mantido.
    /// </summary>
    public static List<T> Filtrar<T>(IEnumerable<T> itens, string? filtro, Func<T, Demanda?> demanda)
        => itens.Where(i => demanda(i) is not { } d || Casa(d, filtro)).ToList();
}

/// <summary>Para onde "Ir para a próxima" leva: um trabalho ou "Sessões" (<see cref="DemandaId"/> nulo).</summary>
public sealed record DestinoProxima(string? DemandaId)
{
    public bool EhSessoes => DemandaId is null;
}

/// <summary>Escolha do próximo trabalho com sessão no status destacado no topo, como função pura.</summary>
public static class ProximaSessao
{
    /// <summary>
    /// Percorre os trabalhos na ordem da lista, depois do selecionado e voltando ao início, e por fim "Sessões"
    /// (que fica no fim da lista), procurando sessão no status destacado. Devolve null se não houver status
    /// destacado ou nenhuma sessão nele. Sem seleção, a busca começa no início da lista. Sessões excluídas já
    /// não estão na visão.
    /// </summary>
    /// <param name="selecionadaId">Identificador do trabalho selecionado, ou null.</param>
    /// <param name="sessoesSelecionada">"Sessões" está selecionada.</param>
    public static DestinoProxima? Escolher(Visao visao, string? selecionadaId, bool sessoesSelecionada)
    {
        if (visao.ResumoCabecalho.StatusDestacado is not { } status) return null;

        // Posição na lista: trabalhos 0..n-1 e "Sessões" em n.
        var candidatos = new List<(DestinoProxima Destino, bool Tem)>();
        foreach (var d in visao.Demandas)
        {
            var tem = d.Cards.SelectMany(c => c.Sessoes).Any(s => s.Status == status);
            candidatos.Add((new DestinoProxima(d.Demanda.Id), tem));
        }
        candidatos.Add((new DestinoProxima(null), visao.Sessoes.TemStatus(status)));

        var atual = -1;
        if (sessoesSelecionada) atual = candidatos.Count - 1;
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
