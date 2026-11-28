using System.Text.Json.Nodes;
using DataPilot.Core.Models;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Persistence;

public class ConfiguracoesTests
{
    private static Configuracoes Ler(string secao)
    {
        var (dados, pasta, _) = DadosPainelTests.Novo($$"""{"demandas":[],"configuracoes":{{secao}}}""");
        using var _ = pasta;
        return dados.Carregar().Configuracoes;
    }

    [Fact]
    public void Sem_secao_vale_o_padrao()
    {
        var (dados, pasta, _) = DadosPainelTests.Novo();
        using var _ = pasta;
        Assert.Equal(new Configuracoes("datapilot-agente"), dados.Carregar().Configuracoes);
    }

    [Theory]
    [InlineData("""{"execucao":"windows","usarSsh":false,"usuarioSsh":"agente-ia","container":"agente_1"}""")]
    [InlineData("""{"execucao":"wsl","usuarioSsh":"-oProxyCommand=calc","container":"agente_1"}""")]
    public void Campos_de_versoes_anteriores_sao_desconsiderados(string secao)
        => Assert.Equal(new Configuracoes("agente_1"), Ler(secao));

    [Theory]
    [InlineData("""{"container":"--privileged"}""")]
    [InlineData("""{"container":2}""")]
    [InlineData("""{"usarSsh":false}""")]
    public void Container_ausente_ou_invalido_vale_o_padrao(string secao)
        => Assert.Equal(Configuracoes.Padrao, Ler(secao));

    [Fact]
    public void Arquivo_ilegivel_usa_o_padrao_e_nao_grava()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo("{ não é json");
        using var _ = pasta;
        Assert.Equal(Configuracoes.Padrao, dados.Carregar().Configuracoes);
        Assert.False(dados.SalvarConfiguracoes(Configuracoes.Padrao).Ok);
        Assert.Equal("{ não é json", File.ReadAllText(caminho));
    }

    [Fact]
    public void Salvar_deixa_so_o_container_na_secao_e_preserva_o_resto()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo(
            """{"demandas":[],"campoDesconhecido":42,"configuracoes":{"execucao":"windows","usarSsh":false,"usuarioSsh":"ia"}}""");
        using var _ = pasta;
        var novas = new Configuracoes("agente_1");

        Assert.True(dados.SalvarConfiguracoes(novas).Ok);

        Assert.Equal(novas, dados.Carregar().Configuracoes);
        var raiz = JsonNode.Parse(File.ReadAllText(caminho))!.AsObject();
        Assert.Equal(["container"], raiz["configuracoes"]!.AsObject().Select(p => p.Key));
        Assert.Equal(42, (int)raiz["campoDesconhecido"]!);
    }

    [Theory]
    [InlineData("--privileged")]
    [InlineData("")]
    public void Container_invalido_e_recusado_sem_gravar(string container)
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo();
        using var _ = pasta;
        Assert.False(dados.SalvarConfiguracoes(new Configuracoes(container)).Ok);
        Assert.False(File.Exists(caminho));
    }
}
