using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Sessions;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Sessions;

/// <summary>Cenários de "Abrir de novo um lançamento pendente" (lancamento-de-sessao).</summary>
public class ReabrirLancamentoTests
{
    private const string L1 = ApoioVinculoAutomatico.L1;
    private static readonly DateTimeOffset Clique = Fabrica.Agora;
    private static readonly DateTimeOffset Depois = Clique.AddMinutes(40);

    private static (AberturaSessao Abertura, DadosPainel Dados, DisparadorFalso Disparador, RelogioFalso Relogio, PastaTemporaria Pasta)
        ComLancamento(string? rotulo = "ajuste")
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        Assert.True(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, rotulo, Clique).Ok);
        var disparador = new DisparadorFalso();
        var relogio = new RelogioFalso(Depois);
        return (new AberturaSessao(dados, disparador, relogio), dados, disparador, relogio, pasta);
    }

    private static RepositorioCadastrado Repo(DadosPainel d) => d.Carregar().Demandas.Single().Repositorios.Single();

    [Fact]
    public void Abre_o_mesmo_comando_de_uma_sessao_nova_com_o_rotulo_do_lancamento()
    {
        var (abertura, dados, disparador, _, pasta) = ComLancamento();
        using var _1 = pasta;

        Assert.True(abertura.ReabrirLancamento(L1).Ok);

        var esperado = MontadorComando.Novo("CHG-142", "CHG-142", Repo(dados), Agente.Codex, null, "ajuste", Fabrica.Windows);
        var comando = Assert.Single(disparador.Comandos);
        Assert.Equal(esperado.Executavel, comando.Executavel);
        Assert.Equal(esperado.Argumentos, comando.Argumentos);
        Assert.DoesNotContain("-Sessao", comando.Argumentos);
    }

    [Fact]
    public void Reaproveita_o_lancamento_e_renova_a_data()
    {
        var (abertura, dados, _, _, pasta) = ComLancamento();
        using var _1 = pasta;

        Assert.True(abertura.ReabrirLancamento(L1).Ok);

        Assert.Equal(new LancamentoPendente(L1, Agente.Codex, Depois, "ajuste"), Assert.Single(Repo(dados).Lancamentos));
        Assert.Empty(Repo(dados).Sessoes);
    }

    [Fact]
    public void A_data_e_renovada_antes_de_iniciar_o_terminal()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _1 = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, Clique);
        DateTimeOffset? vista = null;
        var disparador = new Observador(() => vista = Repo(dados).Lancamentos.Single().CriadoEm);

        new AberturaSessao(dados, disparador, new RelogioFalso(Depois)).ReabrirLancamento(L1);

        Assert.Equal(Depois, vista);
    }

    private sealed class Observador(Action aoIniciar) : IDisparador
    {
        public void Iniciar(ComandoTerminal comando) => aoIniciar();
    }

    [Fact]
    public void Primeira_mensagem_depois_de_abrir_de_novo_vincula_com_o_rotulo()
    {
        var (abertura, dados, _, relogio, pasta) = ComLancamento();
        using var _1 = pasta;
        Assert.True(abertura.ReabrirLancamento(L1).Ok);

        // Sessão criada depois da reabertura corresponde; uma criada entre o primeiro clique e a reabertura, não.
        var antes = ApoioVinculoAutomatico.Codex(ApoioVinculoAutomatico.S2, Clique.AddMinutes(10));
        var nova = ApoioVinculoAutomatico.Codex(ApoioVinculoAutomatico.S1, Depois.AddMinutes(1));
        relogio.Agora = Depois.AddMinutes(2);
        Assert.Equal(1, VinculoAutomatico.Aplicar(dados, dados.Carregar(), [antes, nova], relogio));

        var sessao = Assert.Single(Repo(dados).Sessoes);
        Assert.Equal(ApoioVinculoAutomatico.S1, sessao.SessionId);
        Assert.Equal("ajuste", sessao.Rotulo);
        Assert.Empty(Repo(dados).Lancamentos);
    }

    [Fact]
    public void Terminal_que_nao_inicia_mantem_a_data_anterior()
    {
        var (abertura, dados, disparador, _, pasta) = ComLancamento();
        using var _1 = pasta;
        disparador.Falhar = true;

        var r = abertura.ReabrirLancamento(L1);

        Assert.False(r.Ok);
        Assert.Equal(new LancamentoPendente(L1, Agente.Codex, Clique, "ajuste"), Assert.Single(Repo(dados).Lancamentos));
    }

    [Fact]
    public void Lancamento_que_nao_existe_mais_nao_abre_nem_grava()
    {
        var (abertura, dados, disparador, _, pasta) = ComLancamento();
        using var _1 = pasta;
        Assert.True(dados.DesfazerLancamento(L1).Ok);
        var conteudo = File.ReadAllText(dados.Caminho);

        Assert.False(abertura.ReabrirLancamento(L1).Ok);

        Assert.Empty(disparador.Comandos);
        Assert.Equal(conteudo, File.ReadAllText(dados.Caminho));
    }

    [Fact]
    public void Lancamento_vencido_nao_abre_nem_grava()
    {
        var (abertura, dados, disparador, relogio, pasta) = ComLancamento();
        using var _1 = pasta;
        relogio.Agora = Clique.AddHours(25);
        var conteudo = File.ReadAllText(dados.Caminho);

        Assert.False(abertura.ReabrirLancamento(L1).Ok);

        Assert.Empty(disparador.Comandos);
        Assert.Equal(conteudo, File.ReadAllText(dados.Caminho));
    }

    [Fact]
    public void Lancamento_de_demanda_arquivada_nao_abre()
    {
        var (abertura, dados, disparador, _, pasta) = ComLancamento();
        using var _1 = pasta;
        Assert.True(dados.ArquivarDemanda("CHG-142").Ok);

        Assert.False(abertura.ReabrirLancamento(L1).Ok);

        Assert.Empty(disparador.Comandos);
        Assert.Equal(Clique, Repo(dados).Lancamentos.Single().CriadoEm);
    }

    [Fact]
    public void Abrir_um_lancamento_nao_mexe_no_outro()
    {
        var (abertura, dados, _, _, pasta) = ComLancamento();
        using var _1 = pasta;
        Assert.True(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L2, null, Clique).Ok);

        Assert.True(abertura.ReabrirLancamento(L1).Ok);

        var lancamentos = Repo(dados).Lancamentos;
        Assert.Equal(2, lancamentos.Count);
        Assert.Equal(Depois, lancamentos.Single(l => l.Id == L1).CriadoEm);
        Assert.Equal(Clique, lancamentos.Single(l => l.Id == ApoioVinculoAutomatico.L2).CriadoEm);
    }

    [Fact]
    public void Renovar_lancamento_inexistente_nao_grava()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _1 = pasta;
        var conteudo = File.ReadAllText(dados.Caminho);

        Assert.False(dados.RenovarLancamento(L1, Depois).Ok);

        Assert.Equal(conteudo, File.ReadAllText(dados.Caminho));
    }
}
