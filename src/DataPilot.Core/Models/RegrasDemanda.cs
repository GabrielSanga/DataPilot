namespace DataPilot.Core.Models;

/// <summary>Restrições da demanda fixa "Sem demanda" (a entrada "Sessões") e das demandas arquivadas, em um só lugar.</summary>
public static class RegrasDemanda
{
    public const string IdDemandaImplicita = "sem-demanda";
    public const string TituloDemandaImplicita = "Sem demanda";

    /// <summary>Quanto tempo um lançamento não usado continua valendo.</summary>
    public static readonly TimeSpan ValidadeLancamento = TimeSpan.FromHours(24);

    /// <summary>Lançamento utilizável: a demanda não está arquivada e ele tem até <see cref="ValidadeLancamento"/>.</summary>
    public static bool LancamentoValido(Demanda d, LancamentoPendente l, DateTimeOffset agora)
        => !d.Arquivada && agora - l.CriadoEm <= ValidadeLancamento;

    public static bool EhImplicita(string id)
        => string.Equals(id, IdDemandaImplicita, StringComparison.OrdinalIgnoreCase);

    public static bool PodeEditar(Demanda d) => !EhImplicita(d.Id);
    public static bool PodeArquivar(Demanda d) => !EhImplicita(d.Id) && !d.Arquivada;
    public static bool PodeDesarquivar(Demanda d) => d.Arquivada;
    public static bool PodeVincularRepositorio(Demanda d) => !d.Arquivada;
    public static bool PodeAbrirNovaSessao(Demanda d) => !d.Arquivada && d.Repositorios.Count > 0;
}
