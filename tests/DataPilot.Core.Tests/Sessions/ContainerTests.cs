using System.ComponentModel;
using System.Text;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Sessions;
using DataPilot.Core.Tests.Helpers;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Tests.Sessions;

/// <summary>Comando do container (lancamento-de-sessao) e locais disponíveis nele.</summary>
public class MontadorComandoContainerTests
{
    private const string Id = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    private static ComandoTerminal Novo(string pasta, Agente agente = Agente.Claude, Configuracoes? opcoes = null)
        => MontadorComando.Novo("CHG-142", "Ajuste", new RepositorioCadastrado(pasta, []), agente,
            agente == Agente.Claude ? Id : null, null, opcoes);

    private static SessaoRegistrada Registrada()
        => new(Agente.Claude, Id, "Faturamento", null, Fabrica.Agora, "CHG-142", "Ajuste");

    private static string Caminho(ComandoTerminal c)
        => Encoding.UTF8.GetString(Convert.FromBase64String(c.Argumentos[c.Argumentos.ToList().IndexOf("--caminho") + 1]));

    [Fact]
    public void Claude_novo_no_container()
    {
        var c = Novo("Faturamento");
        Assert.Equal("wt.exe", c.Executavel);
        Assert.Equal("datapilot-agente", c.Container);
        Assert.Equal(["-w", "painel", "nt", "--title"], c.Argumentos.Take(4));
        Assert.Equal(["--suppressApplicationTitle", "docker", "exec", "--interactive", "--tty", "datapilot-agente",
            "/opt/painel/abrir.sh", "--agente", "claude", "--repo", "Faturamento", "--sessao", Id, "--modo", "novo"],
            c.Argumentos.Skip(5));
    }

    [Fact]
    public void Codex_novo_no_container_nao_leva_sessao()
    {
        var c = Novo("Faturamento", Agente.Codex);
        Assert.Equal(["--agente", "codex", "--repo", "Faturamento", "--modo", "novo"], c.Argumentos.TakeLast(6));
        Assert.DoesNotContain("--sessao", c.Argumentos);
    }

    [Fact]
    public void Retomada_do_claude_no_container()
        => Assert.Equal(["--agente", "claude", "--repo", "Faturamento", "--sessao", Id, "--modo", "retomar"],
            MontadorComando.Retomar(Registrada()).Argumentos.TakeLast(8));

    [Theory]
    [InlineData(@"C:\Fontes", "/workspace")]
    [InlineData(@"C:\Fontes\Grupo\Faturamento", "/workspace/Grupo/Faturamento")]
    [InlineData(@"c:\fontes\Meu Projeto;x", "/workspace/Meu Projeto;x")]
    [InlineData(@"C:\openspec\ERP", "/openspec/ERP")]
    [InlineData(@"C:\openspec\erp\Faturamento", "/openspec/ERP/Faturamento")]
    public void Pasta_dos_locais_vai_como_caminho_do_container(string pasta, string esperado)
    {
        var c = Novo(pasta);
        Assert.Equal(esperado, Caminho(c));
        Assert.DoesNotContain("--repo", c.Argumentos);
        Assert.Equal(esperado, LocaisContainer.Traduzir(pasta));
    }

    [Theory]
    [InlineData(@"D:\Projetos\Faturamento")]
    [InlineData(@"C:\FontesAntigas\X")]
    [InlineData(@"C:\openspec")]
    [InlineData(@"C:\openspec\ERPx")]
    public void Pasta_fora_dos_locais_e_recusada(string pasta)
    {
        var e = Assert.Throws<EntradaInvalidaException>(() => Novo(pasta));
        Assert.Equal(LocaisContainer.MensagemFora, e.Message);
        Assert.DoesNotContain("Windows", e.Message);
    }

    [Theory]
    [InlineData("--privileged")]
    [InlineData("")]
    public void Nome_de_container_invalido_e_recusado(string container)
        => Assert.Throws<EntradaInvalidaException>(() => Novo("Faturamento", opcoes: Configuracoes.Padrao with { Container = container }));

    [Theory]
    [InlineData("datapilot-agente", true)]
    [InlineData("agente_1", true)]
    [InlineData("--privileged", false)]
    [InlineData(".x", false)]
    [InlineData("", false)]
    [InlineData("a b", false)]
    [InlineData("a\n", false)]
    public void Validacao_do_nome_do_container(string nome, bool valido) => Assert.Equal(valido, ValidadorEntrada.ContainerValido(nome));

    [Fact]
    public void Nome_do_container_tem_ate_63_caracteres()
    {
        Assert.True(ValidadorEntrada.ContainerValido(new string('a', 63)));
        Assert.False(ValidadorEntrada.ContainerValido(new string('a', 64)));
    }

    [Theory]
    [InlineData("/workspace/Faturamento", "Faturamento", true)]
    [InlineData("/openspec/ERP/Faturamento", @"C:\openspec\ERP\Faturamento", true)]
    [InlineData("/workspace/Grupo/Faturamento/", @"C:\Fontes\Grupo\Faturamento", true)]
    [InlineData("/workspace/Outro", @"C:\openspec\ERP\Faturamento", false)]
    [InlineData("/workspace/Faturamento", @"D:\Projetos\Faturamento", false)]
    public void Sessao_do_container_e_associada_ao_repositorio(string cwd, string repositorio, bool coincide)
        => Assert.Equal(coincide, RegrasVinculo.PastaCoincide(cwd, repositorio));
}

/// <summary>Cenários de abertura e de registro das sessões no container.</summary>
public class AberturaContainerTests
{
    private const string Id = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    private static (AberturaSessao Abertura, DadosPainel Dados, DisparadorFalso Disparador, PastaTemporaria Pasta) Novo()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        var disparador = new DisparadorFalso();
        return (new AberturaSessao(dados, disparador, new RelogioFalso(Fabrica.Agora), () => Guid.Parse(Id)), dados, disparador, pasta);
    }

    private static RepositorioCadastrado Repo(DadosPainel d, string demanda = "CHG-142")
        => d.Carregar().Demandas.Single(x => x.Id == demanda).Repositorios.Single();

    [Fact]
    public void Claude_novo_abre_e_registra_no_container()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.Nova("CHG-142", "Faturamento", Agente.Claude, null).Ok);
        Assert.Single(Repo(dados).Sessoes);
        Assert.Equal("datapilot-agente", Assert.Single(disparador.Comandos).Container);
    }

    [Fact]
    public void Codex_novo_registra_o_lancamento_e_reabre_com_o_nome_em_uso()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.Nova("CHG-142", "Faturamento", Agente.Codex, null).Ok);
        Assert.Single(Repo(dados).Lancamentos);

        dados.SalvarConfiguracoes(new Configuracoes("agente_1"));
        Assert.True(abertura.ReabrirLancamento(Id).Ok);
        Assert.Equal("agente_1", disparador.Comandos[1].Container);
    }

    [Fact]
    public void Retomada_usa_o_nome_do_container_em_uso()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.Nova("CHG-142", "Faturamento", Agente.Claude, null).Ok);
        dados.SalvarConfiguracoes(new Configuracoes("outro"));

        Assert.True(abertura.Retomar(Repo(dados).Sessoes.Single()).Ok);

        Assert.Equal("outro", disparador.Comandos[1].Container);
    }

    [Fact]
    public void Retomada_de_sessao_nao_registrada_registra_e_abre_no_container()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        var pedido = PedidoVinculo.Criar(Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.Zero, Id))!;

        Assert.True(abertura.RetomarNaoRegistrada(pedido, "Faturamento").Ok);

        Assert.Single(Repo(dados, "sem-demanda").Sessoes);
        Assert.Equal("datapilot-agente", Assert.Single(disparador.Comandos).Container);
    }

    [Fact]
    public void Pasta_fora_dos_locais_nao_registra_nem_dispara()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        const string fora = @"D:\Projetos\Faturamento";
        dados.VincularRepositorio("CHG-142", fora);
        var antes = File.ReadAllText(dados.Caminho);

        foreach (var r in new[]
        {
            abertura.Nova("CHG-142", fora, Agente.Claude, null),
            abertura.Nova("CHG-142", fora, Agente.Codex, null),
            abertura.NovaEmSessoes(fora, Agente.Claude, null),
        })
            Assert.Equal(LocaisContainer.MensagemFora, r.Mensagem);

        Assert.Equal(antes, File.ReadAllText(dados.Caminho));
        Assert.Empty(disparador.Comandos);
    }
}

public class DisparadorContainerTests
{
    private static readonly ComandoTerminal NoContainer = new("wt.exe", ["nt"], "datapilot-agente");

    private static ResultadoProcesso Ok(string saida = "") => new(0, saida, "");
    private static ResultadoProcesso Erro(string erro) => new(1, "", erro);

    private sealed class ExecutorFalso(params Func<ResultadoProcesso>[] respostas) : IExecutorProcesso
    {
        private readonly Queue<Func<ResultadoProcesso>> _respostas = new(respostas);
        public List<(string Executavel, string Argumentos, TimeSpan Limite)> Chamadas { get; } = [];

        public ResultadoProcesso Executar(string executavel, IReadOnlyList<string> argumentos, TimeSpan limite)
        {
            Chamadas.Add((executavel, string.Join(' ', argumentos), limite));
            return _respostas.Dequeue()();
        }
    }

    private static string Falha(ExecutorFalso executor)
    {
        var interno = new DisparadorFalso();
        var e = Assert.Throws<FalhaDisparoException>(() => new DisparadorContainer(interno, executor).Iniciar(NoContainer));
        Assert.Empty(interno.Comandos);
        return e.Message;
    }

    [Fact]
    public void Container_em_execucao_abre_direto()
    {
        var (interno, executor) = (new DisparadorFalso(), new ExecutorFalso(() => Ok("true\n")));
        new DisparadorContainer(interno, executor).Iniciar(NoContainer);
        Assert.Single(interno.Comandos);
        Assert.Equal(("docker", "inspect --type container --format {{.State.Running}} datapilot-agente", TimeSpan.FromSeconds(10)),
            Assert.Single(executor.Chamadas));
    }

    [Fact]
    public void Container_parado_e_iniciado()
    {
        var (interno, executor) = (new DisparadorFalso(), new ExecutorFalso(() => Ok("false\n"), () => Ok()));
        new DisparadorContainer(interno, executor).Iniciar(NoContainer);
        Assert.Single(interno.Comandos);
        Assert.Equal(("docker", "start datapilot-agente", TimeSpan.FromSeconds(20)), executor.Chamadas[1]);
    }

    [Fact]
    public void Container_que_nao_inicia()
        => Assert.Contains("Não foi possível iniciar o container", Falha(new(() => Ok("false"), () => Erro("porta em uso"))));

    [Theory]
    [InlineData("Error response from daemon: No such container: datapilot-agente")]
    [InlineData("Error: No such object: datapilot-agente")]
    public void Container_inexistente_orienta_a_subir_o_ambiente(string erro)
    {
        var mensagem = Falha(new(() => Erro(erro)));
        Assert.Contains("docker compose up -d", mensagem);
        Assert.DoesNotContain("Windows", mensagem);
    }

    [Fact]
    public void Docker_parado_orienta_a_abrir_o_docker_desktop()
        => Assert.Contains("Abra o Docker Desktop", Falha(new(() => Erro("failed to connect to the docker API"))));

    [Fact]
    public void Tempo_limite_orienta_a_abrir_o_docker_desktop()
    {
        Assert.Contains("Abra o Docker Desktop", Falha(new(() => throw new TimeoutException())));
        Assert.Contains("Abra o Docker Desktop", Falha(new(() => Ok("false"), () => throw new TimeoutException())));
    }

    [Fact]
    public void Docker_ausente()
    {
        var mensagem = Falha(new(() => throw new Win32Exception(2)));
        Assert.Contains("Não foi possível executar o docker", mensagem);
        Assert.DoesNotContain("Windows", mensagem);
    }

    [Theory]
    [InlineData(Agente.Claude)]
    [InlineData(Agente.Codex)]
    public void Falha_na_verificacao_desfaz_o_registro(Agente agente)
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        var disparador = new DisparadorContainer(new DisparadorFalso(), new ExecutorFalso(() => Erro("No such container")));

        var r = new AberturaSessao(dados, disparador, new RelogioFalso(Fabrica.Agora)).Nova("CHG-142", "Faturamento", agente, null);

        Assert.False(r.Ok);
        var repo = dados.Carregar().Demandas.Single().Repositorios.Single();
        Assert.Empty(repo.Sessoes);
        Assert.Empty(repo.Lancamentos);
    }

    [Fact]
    public void Executor_real_devolve_saida_e_respeita_o_limite()
    {
        var executor = new ExecutorProcesso();
        var r = executor.Executar("cmd.exe", ["/c", "echo", "ok"], TimeSpan.FromSeconds(30));
        Assert.Equal((0, "ok"), (r.Codigo, r.Saida.Trim()));
        Assert.Throws<TimeoutException>(() => executor.Executar("ping.exe", ["-n", "20", "127.0.0.1"], TimeSpan.FromMilliseconds(300)));
        Assert.Throws<Win32Exception>(() => executor.Executar("nao-existe-datapilot.exe", [], TimeSpan.FromSeconds(1)));
    }
}
