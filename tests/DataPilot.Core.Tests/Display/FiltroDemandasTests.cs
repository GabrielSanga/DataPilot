using DataPilot.Core.Display;
using DataPilot.Core.Models;

namespace DataPilot.Core.Tests.Display;

public class FiltroDemandasTests
{
    private static Demanda Dem(string id, string titulo, params (string Pasta, string? Change)[] repos)
        => new(id, titulo, null, null, false, repos.Select(r => new RepositorioCadastrado(r.Pasta, [], [], r.Change)).ToList());

    private static readonly Demanda Chg142 = Dem("CHG-142", "Ajuste NF-e", ("Faturamento", "ajuste-nfe"), ("Estoque", null));
    private static readonly Demanda Chg150 = Dem("CHG-150", "Relatório mensal", ("Relatorios", null));
    private static readonly Demanda Chg160 = Dem("CHG-160", "Migração", ("Faturamento", null));

    private static string[] Ids(string? filtro)
        => new[] { Chg142, Chg150, Chg160 }.Where(d => FiltroDemandas.Casa(d, filtro)).Select(d => d.Id).ToArray();

    [Fact]
    public void PeloRepositorio_MostraSoQuemTemEle()
        => Assert.Equal(["CHG-142"], Ids("estoque"));

    [Fact]
    public void RepositorioEmDoisTrabalhos() => Assert.Equal(["CHG-142", "CHG-160"], Ids("faturamento"));

    [Fact]
    public void SemAcentoNemMaiusculas() => Assert.Equal(["CHG-150"], Ids("RELATORIO MENSAL"));

    [Fact]
    public void ComAcentoNoFiltro_CasaSemAcentoNoTexto() => Assert.Equal(["CHG-160"], Ids("migracao"));

    [Fact]
    public void NaoCasaPeloIdentificador() => Assert.Equal([], Ids("chg-150"));

    [Fact]
    public void PelaChangeDeUmRepositorio() => Assert.Equal(["CHG-142"], Ids("ajuste-nfe"));

    [Fact]
    public void PeloTitulo() => Assert.Equal(["CHG-142"], Ids("nf-e"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FiltroVazio_MostraTudo(string? filtro)
        => Assert.Equal(["CHG-142", "CHG-150", "CHG-160"], Ids(filtro));

    [Fact]
    public void SemResultado_NaoMostraNenhum() => Assert.Equal([], Ids("nada-disso"));

    [Fact]
    public void MantemAOrdemOriginal() => Assert.Equal(["CHG-142"], Ids("ajuste"));
}
