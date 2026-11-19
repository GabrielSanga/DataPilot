using DataPilot.Core;

namespace DataPilot.Core.Tests;

public class ResumoEstruturadoTests
{
    [Theory]
    [InlineData(1, "1 precisa de atenção")]
    [InlineData(2, "2 precisam de atenção")]
    [InlineData(10, "10 precisam de atenção")]
    public void PluralDoAlertaDeAtencao(int n, string esperado)
        => Assert.Equal(esperado, new ResumoCabecalho(n, 0, 0).TextoAlerta);

    [Fact]
    public void ContagensVaoParaOAlertaNaOrdemDeUrgencia()
    {
        var r = new ResumoCabecalho(2, 4, 4);
        Assert.Equal(StatusExibido.PrecisaAtencao, r.StatusDestacado);
        Assert.Equal("2 precisam de atenção", r.TextoAlerta);
        Assert.Equal("4 aguardando você", r.TextoAguardando);
        Assert.Equal("4 executando", r.TextoExecutando);
        Assert.Equal("2 precisam de atenção, 4 aguardando você, 4 executando", r.Texto);
    }

    [Fact]
    public void SoAguardando_DestacaAguardando()
    {
        var r = new ResumoCabecalho(0, 1, 0);
        Assert.Equal(StatusExibido.Aguardando, r.StatusDestacado);
        Assert.Equal("1 aguardando você", r.TextoAlerta);
    }

    [Fact]
    public void SoExecutando_NadaPrecisaDoUsuario()
    {
        var r = new ResumoCabecalho(0, 0, 3);
        Assert.Null(r.StatusDestacado);
        Assert.Equal("Nada precisa de você agora", r.TextoAlerta);
        Assert.Equal("3 executando", r.Texto);
    }

    [Fact]
    public void ResumoNaoMudaQuaisSessoesEntram()
    {
        var relogio = new RelogioFalso(Fabrica.Agora);
        var v = RegrasExibicao.Montar(
        [
            Fabrica.Sessao(StatusGravado.Atencao, TimeSpan.FromMinutes(1), "a"),
            Fabrica.Sessao(StatusGravado.Atencao, TimeSpan.FromMinutes(2), "b"),
            Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromDays(5), "antiga"),
        ], false, relogio);
        Assert.Equal(new ResumoCabecalho(2, 0, 0), RegrasExibicao.Resumo(v));
    }

    [Theory]
    [InlineData(2, 0, "2 repositórios, nenhuma sessão")]
    [InlineData(1, 1, "1 repositório, 1 sessão")]
    [InlineData(2, 3, "2 repositórios, 3 sessões")]
    [InlineData(0, 0, "nenhum repositório, nenhuma sessão")]
    public void TextoDaDemanda(int repositorios, int sessoes, string esperado)
        => Assert.Equal(esperado, VisaoAgrupada.TextoResumo(repositorios, sessoes));
}

public class FiltroDemandasTests
{
    private static Demanda Dem(string id, string titulo, params (string Pasta, string? Change)[] repos)
        => new(id, titulo, null, null, false, repos.Select(r => new RepositorioCadastrado(r.Pasta, [], [], r.Change)).ToList());

    private static readonly Demanda Chg142 = Dem("CHG-142", "Ajuste NF-e", ("Faturamento", "ajuste-nfe"), ("Estoque", null));
    private static readonly Demanda Chg150 = Dem("CHG-150", "Relatório mensal", ("Relatorios", null));
    private static readonly Demanda Chg160 = Dem("CHG-160", "Migração", ("Faturamento", null));

    private static string[] Ids(string filtro)
    {
        // null representa "Sessões".
        Demanda?[] itens = [Chg142, Chg150, Chg160, null];
        return FiltroDemandas.Filtrar(itens, filtro, d => d).Select(d => d?.Id ?? "sessoes").ToArray();
    }

    [Fact]
    public void PeloRepositorio_MostraSoQuemTemEleESessoes()
        => Assert.Equal(["CHG-142", "sessoes"], Ids("estoque"));

    [Fact]
    public void RepositorioEmDoisTrabalhos() => Assert.Equal(["CHG-142", "CHG-160", "sessoes"], Ids("faturamento"));

    [Fact]
    public void SemAcentoNemMaiusculas() => Assert.Equal(["CHG-150", "sessoes"], Ids("RELATORIO MENSAL"));

    [Fact]
    public void ComAcentoNoFiltro_CasaSemAcentoNoTexto() => Assert.Equal(["CHG-160", "sessoes"], Ids("migracao"));

    [Fact]
    public void NaoCasaPeloIdentificador() => Assert.Equal(["sessoes"], Ids("chg-150"));

    [Fact]
    public void PelaChangeDeUmRepositorio() => Assert.Equal(["CHG-142", "sessoes"], Ids("ajuste-nfe"));

    [Fact]
    public void PeloTitulo() => Assert.Equal(["CHG-142", "sessoes"], Ids("nf-e"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FiltroVazio_MostraTudo(string? filtro)
        => Assert.Equal(["CHG-142", "CHG-150", "CHG-160", "sessoes"], FiltroDemandas.Filtrar<Demanda?>([Chg142, Chg150, Chg160, null], filtro, d => d)
            .Select(d => d?.Id ?? "sessoes").ToArray());

    [Fact]
    public void SemResultado_AindaTemSessoes() => Assert.Equal(["sessoes"], Ids("nada-disso"));

    [Fact]
    public void MantemAOrdemOriginal() => Assert.Equal(["CHG-142", "sessoes"], Ids("ajuste"));
}

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
