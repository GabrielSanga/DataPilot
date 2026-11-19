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
