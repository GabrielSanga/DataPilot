using DataPilot.Core.Models;
using DataPilot.Core.Persistence;

namespace DataPilot.Core.Tests.Persistence;

public class ConfiguracoesTests
{
    [Fact]
    public void Sem_secao_vale_o_padrao()
    {
        var (dados, pasta, _) = DadosPainelTests.Novo();
        using var _ = pasta;
        Assert.Equal(new Configuracoes(true, "ia"), dados.Carregar().Configuracoes);
    }

    [Fact]
    public void Usuario_invalido_no_arquivo_vale_o_padrao_e_o_resto_e_lido()
    {
        var (dados, pasta, _) = DadosPainelTests.Novo("""{"demandas":[],"configuracoes":{"usarSsh":false,"usuarioSsh":"-oProxyCommand=calc"}}""");
        using var _ = pasta;
        Assert.Equal(new Configuracoes(false, "ia"), dados.Carregar().Configuracoes);
    }

    [Fact]
    public void Campos_de_tipo_errado_valem_o_padrao()
    {
        var (dados, pasta, _) = DadosPainelTests.Novo("""{"demandas":[],"configuracoes":{"usarSsh":"nao","usuarioSsh":7}}""");
        using var _ = pasta;
        Assert.Equal(Configuracoes.Padrao, dados.Carregar().Configuracoes);
    }

    [Fact]
    public void Arquivo_ilegivel_usa_o_padrao_e_nao_grava()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo("{ não é json");
        using var _ = pasta;
        Assert.Equal(Configuracoes.Padrao, dados.Carregar().Configuracoes);
        Assert.False(dados.SalvarConfiguracoes(new Configuracoes(false, "ia")).Ok);
        Assert.Equal("{ não é json", File.ReadAllText(caminho));
    }

    [Fact]
    public void Grava_e_preserva_os_demais_campos()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo("""{"demandas":[],"campoDesconhecido":42}""");
        using var _ = pasta;
        Assert.True(dados.SalvarConfiguracoes(new Configuracoes(false, "agente-ia")).Ok);
        Assert.Equal(new Configuracoes(false, "agente-ia"), dados.Carregar().Configuracoes);
        Assert.Contains("campoDesconhecido", File.ReadAllText(caminho));
    }

    [Fact]
    public void Usuario_invalido_e_recusado_sem_gravar()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo();
        using var _ = pasta;
        var r = dados.SalvarConfiguracoes(new Configuracoes(true, "-oProxyCommand=calc"));
        Assert.False(r.Ok);
        Assert.False(File.Exists(caminho));
    }
}
