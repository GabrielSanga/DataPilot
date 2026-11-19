using DataPilot.Core.Display;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Display;

public class SessoesNaVisaoTests
{
    private const string IdA = "aaaaaaaa-0000-0000-0000-000000000001";
    private const string IdB = "bbbbbbbb-0000-0000-0000-000000000002";
    private const string IdC = "cccccccc-0000-0000-0000-000000000003";
    private static readonly RelogioFalso Relogio = new(Fabrica.Agora);

    private static SessaoRegistrada Reg(string id, string pasta, string demanda, Agente agente = Agente.Claude)
        => new(agente, id, pasta, null, Fabrica.Agora.AddDays(-1), demanda);

    private static Demanda Dem(string id, params RepositorioCadastrado[] repos)
        => new(id, id, null, null, false, repos);

    private static Sessao Lida(string id, StatusGravado status, Agente agente = Agente.Claude)
        => Fabrica.Sessao(status, TimeSpan.FromMinutes(1), id, agente);

    [Fact]
    public void Sessoes_reune_registradas_lancamentos_e_nao_registradas()
    {
        var lanc = new LancamentoPendente("11111111-2222-3333-4444-555555555555", Agente.Codex, Fabrica.Agora.AddMinutes(-5), null);
        var sem = Dem("sem-demanda", new RepositorioCadastrado("Estoque", [Reg(IdA, "Estoque", "sem-demanda")], [lanc]));
        var trabalho = Dem("t1", new RepositorioCadastrado("Faturamento", [Reg(IdB, "Faturamento", "t1")]));
        var v = VisaoAgrupada.Montar([sem, trabalho],
            [Lida(IdA, StatusGravado.Ativa), Lida(IdB, StatusGravado.Ativa), Lida(IdC, StatusGravado.Atencao)],
            false, false, Relogio);

        Assert.Equal(["t1"], v.Demandas.Select(d => d.Demanda.Id));
        Assert.Equal(IdA, Assert.Single(v.Sessoes.Registradas).Registrada.SessionId);
        Assert.Equal("Estoque", Assert.Single(v.Sessoes.Lancamentos).Pasta);
        Assert.Equal(IdC, Assert.Single(v.Sessoes.NaoRegistradas).Sessao.SessionId);
        Assert.Equal(2, v.Sessoes.Quantidade);
    }

    [Fact]
    public void Sessoes_excluidas_somem_de_todo_lugar_inclusive_do_resumo()
    {
        var sem = Dem("sem-demanda", new RepositorioCadastrado("Estoque", [Reg(IdA, "Estoque", "sem-demanda")]));
        var trabalho = Dem("t1", new RepositorioCadastrado("F", [Reg(IdB, "F", "t1")]));
        var excluidas = new[]
        {
            new SessaoExcluida(Agente.Claude, IdA, Fabrica.Agora),
            new SessaoExcluida(Agente.Claude, IdB.ToUpperInvariant(), Fabrica.Agora),
            new SessaoExcluida(Agente.Claude, IdC, Fabrica.Agora),
        };
        var v = VisaoAgrupada.Montar([sem, trabalho],
            [Lida(IdA, StatusGravado.Atencao), Lida(IdB, StatusGravado.Atencao), Lida(IdC, StatusGravado.Atencao)],
            false, false, Relogio, excluidas);

        Assert.Empty(v.Sessoes.Registradas);
        Assert.Empty(v.Sessoes.NaoRegistradas);
        Assert.Empty(v.Demandas.Single().Cards.Single().Sessoes);
        Assert.Equal(0, v.ResumoCabecalho.PrecisaAtencao);
        Assert.Null(ProximaSessao.Escolher(v, null, false));
    }

    [Fact]
    public void Ir_para_a_proxima_chega_a_Sessoes_quando_so_ha_sessao_la()
    {
        var sem = Dem("sem-demanda", new RepositorioCadastrado("Estoque", [Reg(IdA, "Estoque", "sem-demanda")]));
        var trabalho = Dem("t1", new RepositorioCadastrado("F", [Reg(IdB, "F", "t1")]));
        var v = VisaoAgrupada.Montar([sem, trabalho],
            [Lida(IdA, StatusGravado.Aguardando), Lida(IdB, StatusGravado.Ativa)], false, false, Relogio);
        Assert.True(ProximaSessao.Escolher(v, null, false)!.EhSessoes);
    }

    [Fact]
    public void Sessao_excluida_nao_e_vinculada_por_lancamento()
    {
        var lanc = new LancamentoPendente("11111111-2222-3333-4444-555555555555", Agente.Codex, Fabrica.Agora.AddMinutes(-5), null);
        var d = Dem("t1", new RepositorioCadastrado("App", [], [lanc]));
        var lida = new Sessao(Agente.Codex, IdA, @"C:\Fontes\App", StatusGravado.Ativa, Fabrica.Agora, Fabrica.Agora.AddMinutes(-1));

        Assert.Single(VinculoAutomatico.Calcular([d], [lida], Fabrica.Agora));
        var excluidas = new[] { new SessaoExcluida(Agente.Codex, IdA, Fabrica.Agora) };
        Assert.Empty(VinculoAutomatico.Calcular([d], [lida], Fabrica.Agora, excluidas));
    }
}
