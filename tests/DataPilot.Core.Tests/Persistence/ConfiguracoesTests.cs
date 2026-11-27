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
        Assert.Equal(new Configuracoes(Execucao.Container, "datapilot-agente", true, "ia"), dados.Carregar().Configuracoes);
    }

    [Fact]
    public void Versao_anterior_so_com_ssh_usa_o_container_e_mantem_o_ssh()
        => Assert.Equal(new Configuracoes(Execucao.Container, "datapilot-agente", false, "agente-ia"),
            Ler("""{"usarSsh":false,"usuarioSsh":"agente-ia"}"""));

    [Fact]
    public void Usuario_invalido_no_arquivo_vale_o_padrao_e_o_resto_e_lido()
        => Assert.Equal(Configuracoes.Padrao with { UsarSsh = false }, Ler("""{"usarSsh":false,"usuarioSsh":"-oProxyCommand=calc"}"""));

    [Fact]
    public void Container_invalido_no_arquivo_vale_o_padrao()
        => Assert.Equal(Fabrica.Windows, Ler("""{"execucao":"windows","container":"--privileged"}"""));

    [Fact]
    public void Ambiente_desconhecido_no_arquivo_vale_o_container()
        => Assert.Equal(Execucao.Container, Ler("""{"execucao":"wsl"}""").Execucao);

    [Fact]
    public void Campos_de_tipo_errado_valem_o_padrao()
        => Assert.Equal(Configuracoes.Padrao, Ler("""{"execucao":1,"container":2,"usarSsh":"nao","usuarioSsh":7}"""));

    [Fact]
    public void Arquivo_ilegivel_usa_o_padrao_e_nao_grava()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo("{ não é json");
        using var _ = pasta;
        Assert.Equal(Configuracoes.Padrao, dados.Carregar().Configuracoes);
        Assert.False(dados.SalvarConfiguracoes(Fabrica.Windows).Ok);
        Assert.Equal("{ não é json", File.ReadAllText(caminho));
    }

    [Fact]
    public void Grava_e_preserva_os_demais_campos()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo("""{"demandas":[],"campoDesconhecido":42}""");
        using var _ = pasta;
        var novas = new Configuracoes(Execucao.Windows, "agente_1", false, "agente-ia");
        Assert.True(dados.SalvarConfiguracoes(novas).Ok);
        Assert.Equal(novas, dados.Carregar().Configuracoes);
        var texto = File.ReadAllText(caminho);
        Assert.Contains("campoDesconhecido", texto);
        Assert.Contains("\"execucao\": \"windows\"", texto);
        Assert.Contains("\"container\": \"agente_1\"", texto);
    }

    [Theory]
    [InlineData("--privileged", "ia")]
    [InlineData("", "ia")]
    [InlineData("datapilot-agente", "-oProxyCommand=calc")]
    public void Valor_invalido_e_recusado_sem_gravar(string container, string usuario)
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo();
        using var _ = pasta;
        Assert.False(dados.SalvarConfiguracoes(new Configuracoes(Execucao.Container, container, true, usuario)).Ok);
        Assert.False(File.Exists(caminho));
    }
}
