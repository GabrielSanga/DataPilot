using DataPilot.Core.Models;
using DataPilot.Core.Persistence;

namespace DataPilot.Core.Tests.Helpers;

internal static class ApoioVinculoAutomatico
{
    internal const string L1 = "aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa";
    internal const string L2 = "bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb";
    internal const string L3 = "cccccccc-3333-4333-8333-cccccccccccc";
    internal const string S1 = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    internal const string S2 = "11111111-2222-3333-4444-555555555555";

    internal static readonly DateTimeOffset Base = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    internal static (DadosPainel Dados, PastaTemporaria Pasta) NovoDados(params string[] demandas)
    {
        var pasta = new PastaTemporaria();
        var dados = new DadosPainel(Path.Combine(pasta.Caminho, "demandas.json"));
        dados.SalvarConfiguracoes(Fabrica.Windows);
        foreach (var d in demandas)
        {
            dados.CriarDemanda(d, d, null, Base);
            dados.VincularRepositorio(d, "Faturamento");
        }
        return (dados, pasta);
    }

    internal static Sessao Codex(string id, DateTimeOffset? criado, string cwd = @"C:\Fontes\Faturamento", Agente agente = Agente.Codex)
        => new(agente, id, cwd, StatusGravado.Executando, Base, criado);

    internal static Demanda Demanda(string id, bool arquivada, params LancamentoPendente[] lancamentos)
        => new(id, id, null, null, arquivada, [new RepositorioCadastrado("Faturamento", [], lancamentos)]);

    internal static LancamentoPendente Lanc(string id, DateTimeOffset criado, string? rotulo = null, Agente agente = Agente.Codex)
        => new(id, agente, criado, rotulo);
}
