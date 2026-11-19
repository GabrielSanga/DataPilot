using DataPilot.Core.Models;
using DataPilot.Core.Persistence;

namespace DataPilot.Core.Tests.Helpers;

public static class DadosPainelDeTeste
{
    /// <summary>Cria um trabalho com identificador escolhido, para montar cenários (o painel gera o identificador).</summary>
    public static ResultadoOperacao CriarDemanda(this DadosPainel dados, string id, string titulo, string? change, DateTimeOffset criadaEm)
        => dados.CriarDemandaComId(id, titulo, criadaEm);
}
