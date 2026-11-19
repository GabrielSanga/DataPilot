namespace DataPilot.Wpf.Models;

/// <summary>Trabalho oferecido no diálogo de vínculo, com os repositórios dele e o que coincide com a pasta da sessão (se houver).</summary>
public sealed record OpcaoDemandaVinculo(string Id, string Texto, IReadOnlyList<string> Repositorios, string? RepositorioSugerido);
