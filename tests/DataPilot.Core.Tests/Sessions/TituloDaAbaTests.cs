using DataPilot.Core.Models;
using DataPilot.Core.Sessions;

namespace DataPilot.Core.Tests.Sessions;

public class TituloDaAbaTests
{
    [Fact]
    public void Ponto_e_virgula_no_titulo_vira_virgula()
        => Assert.Equal("Fase 1, ajustes · App", MontadorComando.Titulo("t1", "Fase 1; ajustes", Agente.Claude, "App", null));

    [Fact]
    public void Titulo_com_mais_de_40_caracteres_e_cortado()
    {
        var titulo = MontadorComando.Titulo("t1", new string('x', 60), Agente.Claude, "App", "r");
        Assert.Equal(new string('x', 40) + " · App · r", titulo);
    }

    [Fact]
    public void Titulo_do_trabalho_nao_vai_com_ponto_e_virgula_no_comando()
    {
        var repo = new RepositorioCadastrado("App", []);
        var c = MontadorComando.Novo("t1", "a;b", repo, Agente.Claude, "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10", null);
        Assert.DoesNotContain(";", c.Argumentos[4]);
    }

    [Fact]
    public void Titulo_do_trabalho_invalido_e_recusado()
    {
        var repo = new RepositorioCadastrado("App", []);
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("t1", "", repo, Agente.Codex, null, null));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("t1", null, repo, Agente.Codex, null, null));
    }

    [Fact]
    public void Em_sessoes_o_titulo_nao_e_exigido()
    {
        var repo = new RepositorioCadastrado("App", []);
        var c = MontadorComando.Novo("sem-demanda", null, repo, Agente.Codex, null, null);
        Assert.Equal("Codex · App", c.Argumentos[4]);
    }
}
