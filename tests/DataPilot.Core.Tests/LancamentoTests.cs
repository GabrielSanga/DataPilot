using DataPilot.Core;

namespace DataPilot.Core.Tests;

public sealed class DisparadorFalso : IDisparador
{
    public List<ComandoTerminal> Comandos { get; } = [];
    public bool Falhar { get; set; }

    public void Iniciar(ComandoTerminal comando)
    {
        if (Falhar) throw new FalhaDisparoException("wt.exe ausente");
        Comandos.Add(comando);
    }
}

public class MontadorComandoTests
{
    private const string Id = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    private static readonly RepositorioCadastrado Repo = new("Faturamento", []);

    private static string[] Prefixo(string titulo) =>
    [
        "-w", "painel", "nt", "--title", titulo, "--suppressApplicationTitle",
        "ssh", "-t", "ia@localhost",
        "powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", @"C:\Painel\abrir.ps1",
    ];

    [Fact]
    public void Claude_novo()
    {
        var c = MontadorComando.Novo(Repo, Agente.Claude, Id, null);
        Assert.Equal("wt.exe", c.Executavel);
        Assert.Equal([.. Prefixo("Claude · Faturamento"), "-Agente", "claude", "-Repo", "Faturamento", "-Sessao", Id, "-Modo", "novo"], c.Argumentos);
    }

    [Fact]
    public void Codex_novo_sem_sessao()
    {
        var c = MontadorComando.Novo(Repo, Agente.Codex, null, null);
        Assert.Equal([.. Prefixo("Codex · Faturamento"), "-Agente", "codex", "-Repo", "Faturamento", "-Modo", "novo"], c.Argumentos);
    }

    [Fact]
    public void Claude_retomar()
    {
        var s = new SessaoRegistrada(Agente.Claude, Id, "Faturamento", null, Fabrica.Agora);
        var c = MontadorComando.Retomar(s);
        Assert.Equal([.. Prefixo("Claude · Faturamento"), "-Agente", "claude", "-Repo", "Faturamento", "-Sessao", Id, "-Modo", "retomar"], c.Argumentos);
    }

    [Fact]
    public void Codex_retomar()
    {
        var s = new SessaoRegistrada(Agente.Codex, Id, "Faturamento", null, Fabrica.Agora);
        var c = MontadorComando.Retomar(s);
        Assert.Equal([.. Prefixo("Codex · Faturamento"), "-Agente", "codex", "-Repo", "Faturamento", "-Sessao", Id, "-Modo", "retomar"], c.Argumentos);
    }

    [Fact]
    public void Titulo_sem_rotulo() => Assert.Equal("Claude · Faturamento", MontadorComando.Titulo(Agente.Claude, "Faturamento", null));

    [Fact]
    public void Titulo_com_rotulo() => Assert.Equal("Claude · Faturamento · implementação", MontadorComando.Titulo(Agente.Claude, "Faturamento", "implementação"));

    [Fact]
    public void Titulo_do_codex() => Assert.Equal("Codex · App", MontadorComando.Titulo(Agente.Codex, "App", null));

    [Fact]
    public void Rotulo_vai_para_o_titulo_como_um_argumento()
    {
        var c = MontadorComando.Novo(Repo, Agente.Claude, Id, "duas palavras");
        Assert.Equal("Claude · Faturamento · duas palavras", c.Argumentos[4]);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("a b")]
    [InlineData("a;b")]
    [InlineData("a\\b")]
    [InlineData("")]
    public void Pasta_invalida_e_recusada(string pasta)
    {
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo(new RepositorioCadastrado(pasta, []), Agente.Claude, Id, null));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, Id, pasta, null, Fabrica.Agora)));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("0b6f1c2e5d1a4c8e9f3a2b7d4e6a8c10")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10; calc")]
    public void Identificador_invalido_e_recusado(string id)
    {
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo(Repo, Agente.Claude, id, null));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, id, "App", null, Fabrica.Agora)));
    }

    [Theory]
    [InlineData("a;b")]
    [InlineData("linha\nnova")]
    [InlineData("12345678901234567890123456789012345678901")]
    public void Rotulo_invalido_e_recusado(string rotulo)
    {
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo(Repo, Agente.Claude, Id, rotulo));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, Id, "App", rotulo, Fabrica.Agora)));
    }

    [Fact]
    public void Agente_invalido_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo(Repo, (Agente)7, Id, null));

    [Fact]
    public void Claude_novo_sem_identificador_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo(Repo, Agente.Claude, null, null));

    [Fact]
    public void Codex_novo_com_identificador_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo(Repo, Agente.Codex, Id, null));
}

public class DisparadorProcessoTests
{
    [Fact]
    public void Executavel_inexistente_falha_com_mensagem()
    {
        var e = Assert.Throws<FalhaDisparoException>(() =>
            new DisparadorProcesso().Iniciar(new ComandoTerminal("datapilot-inexistente-xyz.exe", ["a"])));
        Assert.Contains("datapilot-inexistente-xyz.exe", e.Message);
    }
}

public class CorrespondenciaTests
{
    private const string IdRegistrado = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    private static RepositorioCadastrado[] Repos(Agente agente = Agente.Claude) =>
        [new("Faturamento", [new SessaoRegistrada(agente, IdRegistrado, "Faturamento", "implementação", Fabrica.Agora)])];

    [Fact]
    public void Sessao_aberta_pelo_painel_tem_correspondencia()
    {
        var s = Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.Zero, IdRegistrado);
        var r = Correspondencia.Registrada(s, Repos());
        Assert.NotNull(r);
        Assert.Equal("Faturamento", r.Pasta);
    }

    [Fact]
    public void Sessao_do_codex_nao_tem_correspondencia()
    {
        var s = Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.Zero, IdRegistrado, Agente.Codex);
        Assert.Null(Correspondencia.Registrada(s, Repos()));
    }

    [Fact]
    public void Sessao_criada_por_clear_nao_tem_correspondencia()
    {
        var nova = Fabrica.Sessao(StatusGravado.Ativa, TimeSpan.Zero, "99999999-2222-3333-4444-555555555555");
        var original = Fabrica.Sessao(StatusGravado.Ativa, TimeSpan.Zero, IdRegistrado);
        Assert.Null(Correspondencia.Registrada(nova, Repos()));
        Assert.NotNull(Correspondencia.Registrada(original, Repos()));
    }

    [Fact]
    public void Sem_repositorios_nao_ha_correspondencia()
        => Assert.Null(Correspondencia.Registrada(Fabrica.Sessao(StatusGravado.Ativa, TimeSpan.Zero, IdRegistrado), []));

    [Fact]
    public void Pasta_de_trabalho_do_status_nao_influencia_a_correspondencia()
    {
        var s = Fabrica.Sessao(StatusGravado.Ativa, TimeSpan.Zero, IdRegistrado, cwd: @"C:\Windows");
        Assert.Equal("Faturamento", Correspondencia.Registrada(s, Repos())!.Pasta);
    }

    [Theory]
    [InlineData(StatusExibido.PrecisaAtencao, true)]
    [InlineData(StatusExibido.Aguardando, true)]
    [InlineData(StatusExibido.Executando, true)]
    [InlineData(StatusExibido.Ativa, true)]
    [InlineData(StatusExibido.Inativa, false)]
    [InlineData(StatusExibido.Encerrada, false)]
    public void Confirmacao_conforme_o_status(StatusExibido status, bool esperado)
        => Assert.Equal(esperado, Correspondencia.ExigeConfirmacao(status));
}

public class AberturaSessaoTests
{
    private const string IdFixo = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    private static (AberturaSessao Abertura, DadosPainel Dados, DisparadorFalso Disparador, PastaTemporaria Pasta) Novo(bool cadastrar = true)
    {
        var pasta = new PastaTemporaria();
        var dados = new DadosPainel(Path.Combine(pasta.Caminho, "demandas.json"));
        if (cadastrar) dados.Cadastrar("Faturamento");
        var disparador = new DisparadorFalso();
        var abertura = new AberturaSessao(dados, disparador, new RelogioFalso(Fabrica.Agora), () => Guid.Parse(IdFixo));
        return (abertura, dados, disparador, pasta);
    }

    [Fact]
    public void Claude_novo_gera_registra_e_dispara_com_o_mesmo_identificador()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        var r = abertura.Nova("Faturamento", Agente.Claude, "implementação");
        Assert.True(r.Ok);

        var s = dados.Carregar().Repositorios.Single().Sessoes.Single();
        Assert.Equal(IdFixo, s.SessionId);
        Assert.Equal("implementação", s.Rotulo);
        Assert.Equal(Fabrica.Agora, s.CriadaEm);

        var args = Assert.Single(disparador.Comandos).Argumentos;
        Assert.Equal(["-Sessao", IdFixo, "-Modo", "novo"], args.TakeLast(4));
    }

    [Fact]
    public void Codex_novo_nao_registra_nada()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        var antes = File.ReadAllBytes(dados.Caminho);
        Assert.True(abertura.Nova("Faturamento", Agente.Codex, null).Ok);
        Assert.Equal(antes, File.ReadAllBytes(dados.Caminho));
        Assert.DoesNotContain("-Sessao", Assert.Single(disparador.Comandos).Argumentos);
    }

    [Fact]
    public void Falha_ao_iniciar_desfaz_o_registro()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        disparador.Falhar = true;
        var r = abertura.Nova("Faturamento", Agente.Claude, null);
        Assert.False(r.Ok);
        Assert.Contains("wt.exe", r.Mensagem);
        Assert.Empty(dados.Carregar().Repositorios.Single().Sessoes);
    }

    [Fact]
    public void Rotulo_com_ponto_e_virgula_nao_abre_nem_registra()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.False(abertura.Nova("Faturamento", Agente.Claude, "a;b").Ok);
        Assert.Empty(disparador.Comandos);
        Assert.Empty(dados.Carregar().Repositorios.Single().Sessoes);
    }

    [Fact]
    public void Rotulo_em_branco_conta_como_sem_rotulo()
    {
        var (abertura, dados, _, pasta) = Novo();
        using var _p = pasta;
        Assert.True(abertura.Nova("Faturamento", Agente.Claude, "   ").Ok);
        Assert.Null(dados.Carregar().Repositorios.Single().Sessoes.Single().Rotulo);
    }

    [Fact]
    public void Repositorio_nao_cadastrado_nao_abre()
    {
        var (abertura, _, disparador, pasta) = Novo(cadastrar: false);
        using var _ = pasta;
        Assert.False(abertura.Nova("Faturamento", Agente.Claude, null).Ok);
        Assert.Empty(disparador.Comandos);
    }

    [Fact]
    public void Dados_indisponiveis_nao_abre()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        File.WriteAllText(dados.Caminho, "lixo");
        Assert.False(abertura.Nova("Faturamento", Agente.Claude, null).Ok);
        Assert.Empty(disparador.Comandos);
        Assert.Equal("lixo", File.ReadAllText(dados.Caminho));
    }

    [Fact]
    public void Retomar_dispara_com_os_dados_registrados()
    {
        var (abertura, _, disparador, pasta) = Novo();
        using var _ = pasta;
        var s = new SessaoRegistrada(Agente.Claude, IdFixo, "Faturamento", null, Fabrica.Agora);
        Assert.True(abertura.Retomar(s).Ok);
        Assert.Equal(["-Repo", "Faturamento", "-Sessao", IdFixo, "-Modo", "retomar"], Assert.Single(disparador.Comandos).Argumentos.TakeLast(6));
    }

    [Fact]
    public void Retomar_com_valor_invalido_nao_dispara()
    {
        var (abertura, _, disparador, pasta) = Novo();
        using var _ = pasta;
        var r = abertura.Retomar(new SessaoRegistrada(Agente.Claude, IdFixo, "..", null, Fabrica.Agora));
        Assert.False(r.Ok);
        Assert.Empty(disparador.Comandos);
    }

    [Fact]
    public void Retomar_com_falha_informa_o_erro()
    {
        var (abertura, _, disparador, pasta) = Novo();
        using var _ = pasta;
        disparador.Falhar = true;
        var r = abertura.Retomar(new SessaoRegistrada(Agente.Claude, IdFixo, "Faturamento", null, Fabrica.Agora));
        Assert.False(r.Ok);
        Assert.NotNull(r.Mensagem);
    }
}
