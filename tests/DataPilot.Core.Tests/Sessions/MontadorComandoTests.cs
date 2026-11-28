using DataPilot.Core.Models;
using DataPilot.Core.Sessions;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Sessions;

public class MontadorComandoTests
{
    private const string Id = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    private static readonly RepositorioCadastrado Repo = new("Faturamento", []);

    private static string[] Prefixo(string titulo) =>
    [
        "-w", "painel", "nt", "--title", titulo, "--suppressApplicationTitle",
        "docker", "exec", "--interactive", "--tty", "datapilot-agente", "/opt/painel/abrir.sh",
    ];

    [Fact]
    public void Claude_novo()
    {
        var c = MontadorComando.Novo("CHG-142", "Ajuste NF-e", Repo, Agente.Claude, Id, null);
        Assert.Equal("wt.exe", c.Executavel);
        Assert.Equal([.. Prefixo("Ajuste NF-e · Faturamento"), "--agente", "claude", "--repo", "Faturamento", "--sessao", Id, "--modo", "novo"], c.Argumentos);
    }

    [Fact]
    public void Codex_novo_sem_sessao()
    {
        var c = MontadorComando.Novo("CHG-142", "Ajuste NF-e", Repo, Agente.Codex, null, null);
        Assert.Equal([.. Prefixo("Ajuste NF-e · Faturamento"), "--agente", "codex", "--repo", "Faturamento", "--modo", "novo"], c.Argumentos);
    }

    [Fact]
    public void Claude_retomar()
    {
        var s = new SessaoRegistrada(Agente.Claude, Id, "Faturamento", null, Fabrica.Agora, "CHG-142", "Ajuste NF-e");
        var c = MontadorComando.Retomar(s);
        Assert.Equal([.. Prefixo("Ajuste NF-e · Faturamento"), "--agente", "claude", "--repo", "Faturamento", "--sessao", Id, "--modo", "retomar"], c.Argumentos);
    }

    [Fact]
    public void Codex_retomar()
    {
        var s = new SessaoRegistrada(Agente.Codex, Id, "Faturamento", null, Fabrica.Agora, "CHG-142", "Ajuste NF-e");
        var c = MontadorComando.Retomar(s);
        Assert.Equal([.. Prefixo("Ajuste NF-e · Faturamento"), "--agente", "codex", "--repo", "Faturamento", "--sessao", Id, "--modo", "retomar"], c.Argumentos);
    }

    [Fact]
    public void Titulo_sem_rotulo() => Assert.Equal("Ajuste NF-e · Faturamento", MontadorComando.Titulo("CHG-142", "Ajuste NF-e", Agente.Claude, "Faturamento", null));

    [Fact]
    public void Titulo_com_rotulo() => Assert.Equal("Ajuste NF-e · Faturamento · implementação", MontadorComando.Titulo("CHG-142", "Ajuste NF-e", Agente.Claude, "Faturamento", "implementação"));

    [Fact]
    public void Titulo_de_sem_demanda_usa_o_nome_do_agente() => Assert.Equal("Codex · App", MontadorComando.Titulo("sem-demanda", null, Agente.Codex, "App", null));

    [Fact]
    public void Rotulo_vai_para_o_titulo_como_um_argumento()
    {
        var c = MontadorComando.Novo("CHG-142", "Ajuste NF-e", Repo, Agente.Claude, Id, "duas palavras");
        Assert.Equal("Ajuste NF-e · Faturamento · duas palavras", c.Argumentos[4]);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("a b")]
    [InlineData("a;b")]
    [InlineData("a\\b")]
    [InlineData("")]
    public void Pasta_invalida_e_recusada(string pasta)
    {
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", "Ajuste NF-e", new RepositorioCadastrado(pasta, []), Agente.Claude, Id, null));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, Id, pasta, null, Fabrica.Agora, "CHG-142", "Ajuste NF-e")));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("0b6f1c2e5d1a4c8e9f3a2b7d4e6a8c10")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10; calc")]
    public void Identificador_invalido_e_recusado(string id)
    {
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", "Ajuste NF-e", Repo, Agente.Claude, id, null));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, id, "App", null, Fabrica.Agora, "CHG-142", "Ajuste NF-e")));
    }

    [Theory]
    [InlineData("a;b")]
    [InlineData("linha\nnova")]
    [InlineData("12345678901234567890123456789012345678901")]
    public void Rotulo_invalido_e_recusado(string rotulo)
    {
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", "Ajuste NF-e", Repo, Agente.Claude, Id, rotulo));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, Id, "App", rotulo, Fabrica.Agora, "CHG-142", "Ajuste NF-e")));
    }

    [Fact]
    public void Agente_invalido_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", "Ajuste NF-e", Repo, (Agente)7, Id, null));

    [Fact]
    public void Claude_novo_sem_identificador_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", "Ajuste NF-e", Repo, Agente.Claude, null, null));

    [Fact]
    public void Codex_novo_com_identificador_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", "Ajuste NF-e", Repo, Agente.Codex, Id, null));
}
