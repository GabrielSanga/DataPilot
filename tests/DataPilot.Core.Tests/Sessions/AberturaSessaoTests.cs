using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Sessions;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Sessions;

public class AberturaSessaoTests
{
    private const string IdFixo = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    private static (AberturaSessao Abertura, DadosPainel Dados, DisparadorFalso Disparador, PastaTemporaria Pasta) Novo(bool cadastrar = true)
    {
        var pasta = new PastaTemporaria();
        var dados = new DadosPainel(Path.Combine(pasta.Caminho, "demandas.json"));
        dados.CriarDemanda("CHG-142", "Ajuste NF-e", null, Fabrica.Agora);
        if (cadastrar) dados.VincularRepositorio("CHG-142", "Faturamento");
        var disparador = new DisparadorFalso();
        var abertura = new AberturaSessao(dados, disparador, new RelogioFalso(Fabrica.Agora), () => Guid.Parse(IdFixo));
        return (abertura, dados, disparador, pasta);
    }

    [Fact]
    public void Claude_novo_gera_registra_e_dispara_com_o_mesmo_identificador()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        var r = abertura.Nova("CHG-142", "Faturamento", Agente.Claude, "implementação");
        Assert.True(r.Ok);

        var s = dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes.Single();
        Assert.Equal(IdFixo, s.SessionId);
        Assert.Equal("implementação", s.Rotulo);
        Assert.Equal(Fabrica.Agora, s.CriadaEm);

        var args = Assert.Single(disparador.Comandos).Argumentos;
        Assert.Equal(["--sessao", IdFixo, "--modo", "novo"], args.TakeLast(4));
    }

    [Fact]
    public void Codex_novo_nao_registra_sessao_e_registra_lancamento()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.Nova("CHG-142", "Faturamento", Agente.Codex, "ajuste").Ok);

        var repo = dados.Carregar().Demandas.Single().Repositorios.Single();
        Assert.Empty(repo.Sessoes);
        Assert.Equal(new LancamentoPendente(IdFixo, Agente.Codex, Fabrica.Agora, "ajuste"), Assert.Single(repo.Lancamentos));
        Assert.DoesNotContain("--sessao", Assert.Single(disparador.Comandos).Argumentos);
    }

    [Fact]
    public void Falha_ao_iniciar_desfaz_o_registro()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        disparador.Falhar = true;
        var r = abertura.Nova("CHG-142", "Faturamento", Agente.Claude, null);
        Assert.False(r.Ok);
        Assert.Contains("wt.exe", r.Mensagem);
        Assert.Empty(dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes);
    }

    [Fact]
    public void Rotulo_com_ponto_e_virgula_nao_abre_nem_registra()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.False(abertura.Nova("CHG-142", "Faturamento", Agente.Claude, "a;b").Ok);
        Assert.Empty(disparador.Comandos);
        Assert.Empty(dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes);
    }

    [Fact]
    public void Rotulo_em_branco_conta_como_sem_rotulo()
    {
        var (abertura, dados, _, pasta) = Novo();
        using var _p = pasta;
        Assert.True(abertura.Nova("CHG-142", "Faturamento", Agente.Claude, "   ").Ok);
        Assert.Null(dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes.Single().Rotulo);
    }

    [Fact]
    public void Repositorio_nao_cadastrado_nao_abre()
    {
        var (abertura, _, disparador, pasta) = Novo(cadastrar: false);
        using var _ = pasta;
        Assert.False(abertura.Nova("CHG-142", "Faturamento", Agente.Claude, null).Ok);
        Assert.Empty(disparador.Comandos);
    }

    [Fact]
    public void Dados_indisponiveis_nao_abre()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        File.WriteAllText(dados.Caminho, "lixo");
        Assert.False(abertura.Nova("CHG-142", "Faturamento", Agente.Claude, null).Ok);
        Assert.Empty(disparador.Comandos);
        Assert.Equal("lixo", File.ReadAllText(dados.Caminho));
    }

    [Fact]
    public void Retomar_dispara_com_os_dados_registrados()
    {
        var (abertura, _, disparador, pasta) = Novo();
        using var _ = pasta;
        var s = new SessaoRegistrada(Agente.Claude, IdFixo, "Faturamento", null, Fabrica.Agora, "CHG-142", "Ajuste NF-e");
        Assert.True(abertura.Retomar(s).Ok);
        Assert.Equal(["--repo", "Faturamento", "--sessao", IdFixo, "--modo", "retomar"], Assert.Single(disparador.Comandos).Argumentos.TakeLast(6));
    }

    [Fact]
    public void Retomar_com_valor_invalido_nao_dispara()
    {
        var (abertura, _, disparador, pasta) = Novo();
        using var _ = pasta;
        var r = abertura.Retomar(new SessaoRegistrada(Agente.Claude, IdFixo, "..", null, Fabrica.Agora, "CHG-142", "Ajuste NF-e"));
        Assert.False(r.Ok);
        Assert.Empty(disparador.Comandos);
    }

    [Fact]
    public void Retomar_com_falha_informa_o_erro()
    {
        var (abertura, _, disparador, pasta) = Novo();
        using var _ = pasta;
        disparador.Falhar = true;
        var r = abertura.Retomar(new SessaoRegistrada(Agente.Claude, IdFixo, "Faturamento", null, Fabrica.Agora, "CHG-142", "Ajuste NF-e"));
        Assert.False(r.Ok);
        Assert.NotNull(r.Mensagem);
    }
}
