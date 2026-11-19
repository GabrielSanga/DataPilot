using DataPilot.Core.Display;
using DataPilot.Core.Models;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Display;

public class ProximaSessaoTests
{
    private static readonly RelogioFalso Relogio = new(Fabrica.Agora);

    private static string Guid(int n) => $"aaaaaaaa-0000-0000-0000-{n:D12}";

    private static SessaoRegistrada Reg(int n, string pasta, string demanda)
        => new(Agente.Claude, Guid(n), pasta, null, Fabrica.Agora - TimeSpan.FromDays(1), demanda);

    private static Demanda Dem(string id, params SessaoRegistrada[] sessoes)
        => new(id, id.ToUpperInvariant(), null, null, false,
            sessoes.Length == 0 ? [] : [new RepositorioCadastrado("X", sessoes)]);

    private static Sessao Lida(int n, StatusGravado status, int minutos = 1)
        => Fabrica.Sessao(status, TimeSpan.FromMinutes(minutos), Guid(n));

    private static Visao Montar(Demanda[] demandas, params Sessao[] lidas)
        => VisaoAgrupada.Montar(demandas, lidas, false, false, Relogio);

    [Fact]
    public void ProximaDepoisDaSelecionada()
    {
        var v = Montar(
            [Dem("a", Reg(1, "X", "a")), Dem("b", Reg(2, "X", "b")), Dem("c", Reg(3, "X", "c"))],
            Lida(1, StatusGravado.Atencao, 1), Lida(2, StatusGravado.Atencao, 2), Lida(3, StatusGravado.Atencao, 3));
        var ordem = v.Demandas.Select(d => d.Demanda.Id).ToArray();
        var atual = ordem[0];
        var proxima = ProximaSessao.Escolher(v, atual, false);
        Assert.Equal(ordem[1], proxima!.DemandaId);
    }

    [Fact]
    public void SelecionadaSemAtencao_PulaParaAProximaComAtencao()
    {
        var v = Montar(
            [Dem("a", Reg(1, "X", "a")), Dem("b", Reg(2, "X", "b"))],
            Lida(1, StatusGravado.Executando), Lida(2, StatusGravado.Atencao));
        // "a" tem só sessão executando; o destaque exige atenção em "b".
        Assert.Equal("b", ProximaSessao.Escolher(v, "a", false)!.DemandaId);
    }

    [Fact]
    public void FimDaLista_VoltaAoInicio()
    {
        var v = Montar(
            [Dem("a", Reg(1, "X", "a")), Dem("b", Reg(2, "X", "b"))],
            Lida(1, StatusGravado.Atencao, 1), Lida(2, StatusGravado.Atencao, 2));
        var ordem = v.Demandas.Select(d => d.Demanda.Id).ToArray();
        Assert.Equal(ordem[0], ProximaSessao.Escolher(v, ordem[^1], false)!.DemandaId);
    }

    [Fact]
    public void SemSelecao_ComecaNoInicio()
    {
        var v = Montar([Dem("a", Reg(1, "X", "a"))], Lida(1, StatusGravado.Atencao));
        Assert.Equal("a", ProximaSessao.Escolher(v, null, false)!.DemandaId);
    }

    [Fact]
    public void AtencaoVenceAguardando()
    {
        var v = Montar(
            [Dem("a", Reg(1, "X", "a")), Dem("b", Reg(2, "X", "b"))],
            Lida(1, StatusGravado.Aguardando), Lida(2, StatusGravado.Atencao));
        Assert.Equal("b", ProximaSessao.Escolher(v, null, false)!.DemandaId);
    }

    [Fact]
    public void SoAguardando_UsaAguardando()
    {
        var v = Montar([Dem("a", Reg(1, "X", "a"))], Lida(1, StatusGravado.Aguardando));
        Assert.Equal("a", ProximaSessao.Escolher(v, null, false)!.DemandaId);
    }

    [Fact]
    public void SessaoNaoVinculadaPedindoAtencao_LevaANaoVinculadas()
    {
        var v = Montar([Dem("a", Reg(1, "X", "a"))], Lida(1, StatusGravado.Executando), Lida(9, StatusGravado.Atencao));
        var destino = ProximaSessao.Escolher(v, "a", false);
        Assert.NotNull(destino);
        Assert.True(destino!.EhSessoes);
    }

    [Fact]
    public void NaoVinculadasSelecionada_DaAVoltaParaAPrimeiraDemanda()
    {
        var v = Montar([Dem("a", Reg(1, "X", "a"))], Lida(1, StatusGravado.Atencao), Lida(9, StatusGravado.Atencao));
        Assert.Equal("a", ProximaSessao.Escolher(v, null, true)!.DemandaId);
    }

    [Fact]
    public void SomenteASelecionadaTemAtencao_ContinuaNela()
    {
        var v = Montar([Dem("a", Reg(1, "X", "a")), Dem("b", Reg(2, "X", "b"))], Lida(1, StatusGravado.Atencao), Lida(2, StatusGravado.Ativa));
        Assert.Equal("a", ProximaSessao.Escolher(v, "a", false)!.DemandaId);
    }

    [Fact]
    public void NadaPrecisaDoUsuario_SemAcao()
    {
        var v = Montar([Dem("a", Reg(1, "X", "a"))], Lida(1, StatusGravado.Executando));
        Assert.Null(ProximaSessao.Escolher(v, null, false));
    }
}
