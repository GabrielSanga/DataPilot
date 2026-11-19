using DataPilot.Core.Models;
using DataPilot.Core.Sessions;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Sessions;

public class MontadorComandoDemandaTests
{
    private const string Id = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    [Fact]
    public void Retomar_sessao_de_sem_demanda_usa_o_nome_do_agente_no_titulo()
    {
        var s = new SessaoRegistrada(Agente.Claude, Id, "Faturamento", null, Fabrica.Agora, "sem-demanda");
        Assert.Equal("Claude · Faturamento", MontadorComando.Retomar(s).Argumentos[4]);
    }

    [Fact]
    public void Retomar_sessao_de_trabalho_com_rotulo_comeca_pelo_titulo()
    {
        var s = new SessaoRegistrada(Agente.Claude, Id, "Faturamento", "implementação", Fabrica.Agora, "CHG-142", "Ajuste NF-e");
        Assert.Equal("Ajuste NF-e · Faturamento · implementação", MontadorComando.Retomar(s).Argumentos[4]);
    }

    [Fact]
    public void Novo_do_codex_em_trabalho_usa_o_titulo_na_aba()
    {
        var c = MontadorComando.Novo("CHG-142", "Ajuste NF-e", new RepositorioCadastrado("Estoque", []), Agente.Codex, null, "teste");
        Assert.Equal("Ajuste NF-e · Estoque · teste", c.Argumentos[4]);
    }

    [Theory]
    [InlineData("a;b")]
    [InlineData("com espaço")]
    [InlineData("..")]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Identificador_de_trabalho_invalido_e_recusado(string demandaId)
    {
        Assert.Throws<EntradaInvalidaException>(() =>
            MontadorComando.Novo(demandaId, "Ajuste NF-e", new RepositorioCadastrado("App", []), Agente.Claude, Id, null));
        Assert.Throws<EntradaInvalidaException>(() =>
            MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, Id, "App", null, Fabrica.Agora, demandaId, "Ajuste NF-e")));
    }
}
