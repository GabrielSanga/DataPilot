using System.Text.Json.Nodes;
using DataPilot.Core.Persistence;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Persistence;

public class ChangeNoRepositorioTests
{
    private static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) Novo(string? json = null)
        => DadosPainelTests.Novo(json);

    [Fact]
    public void Change_escolhida_fica_no_repositorio_e_nao_na_demanda()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("X", Fabrica.Agora).Id!;
        Assert.True(dados.VincularRepositorio(id, "Faturamento", "ajuste-boleto").Ok);

        var demanda = JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!;
        Assert.Equal("ajuste-boleto", (string?)demanda["repositorios"]![0]!["openspecChange"]);
        Assert.Null(demanda["openspecChange"]);
        Assert.Equal("ajuste-boleto", dados.Carregar().Demandas.Single().Repositorios.Single().OpenspecChange);
    }

    [Fact]
    public void Trocar_a_change_grava_so_o_repositorio()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("X", Fabrica.Agora).Id!;
        dados.VincularRepositorio(id, "A", "uma");
        dados.VincularRepositorio(id, "B", "outra");
        Assert.True(dados.DefinirChange(id, "A", "nova").Ok);
        var repos = dados.Carregar().Demandas.Single().Repositorios;
        Assert.Equal("nova", repos.Single(r => r.Pasta == "A").OpenspecChange);
        Assert.Equal("outra", repos.Single(r => r.Pasta == "B").OpenspecChange);
    }

    [Fact]
    public void Nenhuma_remove_a_change_do_repositorio()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("X", Fabrica.Agora).Id!;
        dados.VincularRepositorio(id, "A", "uma");
        Assert.True(dados.DefinirChange(id, "A", null).Ok);
        Assert.Null(dados.Carregar().Demandas.Single().Repositorios.Single().OpenspecChange);
    }

    [Fact]
    public void Change_invalida_e_recusada_sem_alterar()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("X", Fabrica.Agora).Id!;
        dados.VincularRepositorio(id, "A");
        var antes = File.ReadAllText(caminho);
        Assert.False(dados.DefinirChange(id, "A", "a b").Ok);
        Assert.False(dados.VincularRepositorio(id, "B", "a;b").Ok);
        Assert.False(dados.DefinirChange(id, "Inexistente", "x").Ok);
        Assert.Equal(antes, File.ReadAllText(caminho));
    }

    [Fact]
    public void Arquivo_com_a_change_na_demanda_vale_para_os_repositorios()
    {
        var (dados, pasta, _) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","openspecChange":"antiga","repositorios":[{"pasta":"X"},{"pasta":"Y"}]}]}""");
        using var _p = pasta;
        var d = dados.Carregar().Demandas.Single();
        Assert.All(d.Repositorios, r => Assert.Equal("antiga", r.OpenspecChange));
    }

    [Fact]
    public void Repositorio_com_change_propria_usa_a_propria()
    {
        var (dados, pasta, _) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","openspecChange":"antiga","repositorios":[{"pasta":"X","openspecChange":"propria"},{"pasta":"Y"}]}]}""");
        using var _p = pasta;
        var repos = dados.Carregar().Demandas.Single().Repositorios;
        Assert.Equal("propria", repos[0].OpenspecChange);
        Assert.Equal("antiga", repos[1].OpenspecChange);
    }

    [Fact]
    public void Change_invalida_no_repositorio_e_tratada_como_ausente()
    {
        var (dados, pasta, _) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","repositorios":[{"pasta":"X","openspecChange":"a b"}]}]}""");
        using var _p = pasta;
        Assert.Null(dados.Carregar().Demandas.Single().Repositorios.Single().OpenspecChange);
    }

    [Fact]
    public void Nenhuma_vence_a_change_antiga_da_demanda()
    {
        var (dados, pasta, _) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","openspecChange":"antiga","repositorios":[{"pasta":"X"}]}]}""");
        using var _p = pasta;
        Assert.True(dados.DefinirChange("A", "X", null).Ok);
        Assert.Null(dados.Carregar().Demandas.Single().Repositorios.Single().OpenspecChange);
    }

    [Fact]
    public void Gravar_preserva_a_change_antiga_da_demanda()
    {
        var (dados, pasta, caminho) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","openspecChange":"antiga","repositorios":[{"pasta":"X"}]}]}""");
        using var _p = pasta;
        dados.DefinirChange("A", "X", "nova");
        Assert.Equal("antiga", (string?)JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!["openspecChange"]);
    }
}
