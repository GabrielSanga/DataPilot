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
        var c = MontadorComando.Novo("CHG-142", Repo, Agente.Claude, Id, null);
        Assert.Equal("wt.exe", c.Executavel);
        Assert.Equal([.. Prefixo("CHG-142 · Faturamento"), "-Agente", "claude", "-Repo", "Faturamento", "-Sessao", Id, "-Modo", "novo"], c.Argumentos);
    }

    [Fact]
    public void Codex_novo_sem_sessao()
    {
        var c = MontadorComando.Novo("CHG-142", Repo, Agente.Codex, null, null);
        Assert.Equal([.. Prefixo("CHG-142 · Faturamento"), "-Agente", "codex", "-Repo", "Faturamento", "-Modo", "novo"], c.Argumentos);
    }

    [Fact]
    public void Claude_retomar()
    {
        var s = new SessaoRegistrada(Agente.Claude, Id, "Faturamento", null, Fabrica.Agora, "CHG-142");
        var c = MontadorComando.Retomar(s);
        Assert.Equal([.. Prefixo("CHG-142 · Faturamento"), "-Agente", "claude", "-Repo", "Faturamento", "-Sessao", Id, "-Modo", "retomar"], c.Argumentos);
    }

    [Fact]
    public void Codex_retomar()
    {
        var s = new SessaoRegistrada(Agente.Codex, Id, "Faturamento", null, Fabrica.Agora, "CHG-142");
        var c = MontadorComando.Retomar(s);
        Assert.Equal([.. Prefixo("CHG-142 · Faturamento"), "-Agente", "codex", "-Repo", "Faturamento", "-Sessao", Id, "-Modo", "retomar"], c.Argumentos);
    }

    [Fact]
    public void Titulo_sem_rotulo() => Assert.Equal("CHG-142 · Faturamento", MontadorComando.Titulo("CHG-142", Agente.Claude, "Faturamento", null));

    [Fact]
    public void Titulo_com_rotulo() => Assert.Equal("CHG-142 · Faturamento · implementação", MontadorComando.Titulo("CHG-142", Agente.Claude, "Faturamento", "implementação"));

    [Fact]
    public void Titulo_de_sem_demanda_usa_o_nome_do_agente() => Assert.Equal("Codex · App", MontadorComando.Titulo("sem-demanda", Agente.Codex, "App", null));

    [Fact]
    public void Rotulo_vai_para_o_titulo_como_um_argumento()
    {
        var c = MontadorComando.Novo("CHG-142", Repo, Agente.Claude, Id, "duas palavras");
        Assert.Equal("CHG-142 · Faturamento · duas palavras", c.Argumentos[4]);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("a b")]
    [InlineData("a;b")]
    [InlineData("a\\b")]
    [InlineData("")]
    public void Pasta_invalida_e_recusada(string pasta)
    {
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", new RepositorioCadastrado(pasta, []), Agente.Claude, Id, null));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, Id, pasta, null, Fabrica.Agora, "CHG-142")));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("0b6f1c2e5d1a4c8e9f3a2b7d4e6a8c10")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10; calc")]
    public void Identificador_invalido_e_recusado(string id)
    {
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", Repo, Agente.Claude, id, null));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, id, "App", null, Fabrica.Agora, "CHG-142")));
    }

    [Theory]
    [InlineData("a;b")]
    [InlineData("linha\nnova")]
    [InlineData("12345678901234567890123456789012345678901")]
    public void Rotulo_invalido_e_recusado(string rotulo)
    {
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", Repo, Agente.Claude, Id, rotulo));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, Id, "App", rotulo, Fabrica.Agora, "CHG-142")));
    }

    [Fact]
    public void Agente_invalido_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", Repo, (Agente)7, Id, null));

    [Fact]
    public void Claude_novo_sem_identificador_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", Repo, Agente.Claude, null, null));

    [Fact]
    public void Codex_novo_com_identificador_e_recusado()
        => Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("CHG-142", Repo, Agente.Codex, Id, null));
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
    public void Retomar_sessao_de_demanda_com_rotulo_comeca_pelo_id()
    {
        var s = new SessaoRegistrada(Agente.Claude, Id, "Faturamento", "implementação", Fabrica.Agora, "CHG-142");
        Assert.Equal("CHG-142 · Faturamento · implementação", MontadorComando.Retomar(s).Argumentos[4]);
    }

    [Fact]
    public void Novo_do_codex_em_demanda_usa_o_id_no_titulo()
    {
        var c = MontadorComando.Novo("CHG-142", new RepositorioCadastrado("Estoque", []), Agente.Codex, null, "teste");
        Assert.Equal("CHG-142 · Estoque · teste", c.Argumentos[4]);
    }

    [Theory]
    [InlineData("a;b")]
    [InlineData("com espaço")]
    [InlineData("..")]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Identificador_de_demanda_invalido_e_recusado(string demandaId)
    {
        Assert.Throws<EntradaInvalidaException>(() =>
            MontadorComando.Novo(demandaId, new RepositorioCadastrado("App", []), Agente.Claude, Id, null));
        Assert.Throws<EntradaInvalidaException>(() =>
            MontadorComando.Retomar(new SessaoRegistrada(Agente.Claude, Id, "App", null, Fabrica.Agora, demandaId)));
    }
}

public class CorrespondenciaTests
{
    [Theory]
    [InlineData(StatusExibido.PrecisaAtencao, true)]
    [InlineData(StatusExibido.Aguardando, true)]
    [InlineData(StatusExibido.Executando, true)]
    [InlineData(StatusExibido.Ativa, true)]
    [InlineData(StatusExibido.Inativa, false)]
    [InlineData(StatusExibido.Encerrada, false)]
    [InlineData(StatusExibido.SemStatus, false)]
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
        Assert.Equal(["-Sessao", IdFixo, "-Modo", "novo"], args.TakeLast(4));
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
        Assert.DoesNotContain("-Sessao", Assert.Single(disparador.Comandos).Argumentos);
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
        var s = new SessaoRegistrada(Agente.Claude, IdFixo, "Faturamento", null, Fabrica.Agora, "CHG-142");
        Assert.True(abertura.Retomar(s).Ok);
        Assert.Equal(["-Repo", "Faturamento", "-Sessao", IdFixo, "-Modo", "retomar"], Assert.Single(disparador.Comandos).Argumentos.TakeLast(6));
    }

    [Fact]
    public void Retomar_com_valor_invalido_nao_dispara()
    {
        var (abertura, _, disparador, pasta) = Novo();
        using var _ = pasta;
        var r = abertura.Retomar(new SessaoRegistrada(Agente.Claude, IdFixo, "..", null, Fabrica.Agora, "CHG-142"));
        Assert.False(r.Ok);
        Assert.Empty(disparador.Comandos);
    }

    [Fact]
    public void Retomar_com_falha_informa_o_erro()
    {
        var (abertura, _, disparador, pasta) = Novo();
        using var _ = pasta;
        disparador.Falhar = true;
        var r = abertura.Retomar(new SessaoRegistrada(Agente.Claude, IdFixo, "Faturamento", null, Fabrica.Agora, "CHG-142"));
        Assert.False(r.Ok);
        Assert.NotNull(r.Mensagem);
    }
}

public class AberturaSessaoDemandaTests
{
    private const string IdFixo = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    private static (AberturaSessao Abertura, DadosPainel Dados, DisparadorFalso Disparador, PastaTemporaria Pasta) Novo()
    {
        var pasta = new PastaTemporaria();
        var dados = new DadosPainel(Path.Combine(pasta.Caminho, "demandas.json"));
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.CriarDemanda("B", "B", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "Faturamento");
        dados.VincularRepositorio("B", "Faturamento");
        var disparador = new DisparadorFalso();
        return (new AberturaSessao(dados, disparador, new RelogioFalso(Fabrica.Agora), () => Guid.Parse(IdFixo)), dados, disparador, pasta);
    }

    [Fact]
    public void Mesma_pasta_em_duas_demandas_registra_na_demanda_indicada()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.Nova("B", "Faturamento", Agente.Claude, null).Ok);

        var demandas = dados.Carregar().Demandas;
        Assert.Empty(demandas.Single(d => d.Id == "A").Repositorios.Single().Sessoes);
        var s = demandas.Single(d => d.Id == "B").Repositorios.Single().Sessoes.Single();
        Assert.Equal(IdFixo, s.SessionId);
        Assert.Equal("B", s.DemandaId);
        Assert.Equal("B · Faturamento", Assert.Single(disparador.Comandos).Argumentos[4]);
    }

    [Fact]
    public void Titulo_da_aba_leva_o_id_da_demanda_e_o_rotulo()
    {
        var (abertura, _, disparador, pasta) = Novo();
        using var _ = pasta;
        abertura.Nova("A", "Faturamento", Agente.Claude, "implementação");
        Assert.Equal("A · Faturamento · implementação", Assert.Single(disparador.Comandos).Argumentos[4]);
    }

    [Fact]
    public void Demanda_arquivada_ou_inexistente_nao_abre()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        dados.ArquivarDemanda("A");
        Assert.False(abertura.Nova("A", "Faturamento", Agente.Claude, null).Ok);
        Assert.False(abertura.Nova("Z", "Faturamento", Agente.Claude, null).Ok);
        Assert.Empty(disparador.Comandos);
    }

    [Fact]
    public void Repositorio_de_outra_demanda_nao_abre()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        dados.VincularRepositorio("A", "Estoque");
        Assert.False(abertura.Nova("B", "Estoque", Agente.Claude, null).Ok);
        Assert.Empty(disparador.Comandos);
    }

    [Fact]
    public void Retomar_sessao_movida_abre_no_mesmo_repositorio()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        abertura.Nova("A", "Faturamento", Agente.Claude, null);
        dados.CriarDemanda("C", "C", null, Fabrica.Agora);
        Assert.True(dados.MoverSessao(IdFixo, "C").Ok);

        var sessao = dados.Carregar().Demandas.Single(d => d.Id == "C").Repositorios.Single().Sessoes.Single();
        Assert.True(abertura.Retomar(sessao).Ok);
        var args = disparador.Comandos.Last().Argumentos;
        Assert.Equal("C · Faturamento", args[4]);
        Assert.Equal(["-Repo", "Faturamento", "-Sessao", IdFixo, "-Modo", "retomar"], args.TakeLast(6));
    }
}
