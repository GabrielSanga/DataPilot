using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Sessions;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Sessions;

public class AberturaCodexLancamentoTests
{
    private const string IdFixo = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    private static (AberturaSessao Abertura, DadosPainel Dados, DisparadorFalso Disparador, PastaTemporaria Pasta) Novo()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        var disparador = new DisparadorFalso();
        return (new AberturaSessao(dados, disparador, new RelogioFalso(Fabrica.Agora), () => Guid.Parse(IdFixo)), dados, disparador, pasta);
    }

    private static RepositorioCadastrado Repo(DadosPainel d) => d.Carregar().Demandas.Single().Repositorios.Single();

    [Fact]
    public void Codex_novo_registra_o_lancamento_com_agente_rotulo_e_data()
    {
        var (abertura, dados, _, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.Nova("CHG-142", "Faturamento", Agente.Codex, "ajuste").Ok);
        Assert.Equal(new LancamentoPendente(IdFixo, Agente.Codex, Fabrica.Agora, "ajuste"), Assert.Single(Repo(dados).Lancamentos));
        Assert.Empty(Repo(dados).Sessoes);
    }

    [Fact]
    public void Lancamento_e_registrado_antes_de_iniciar_o_terminal()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        var vistos = -1;
        var disparador = new Observador(() => vistos = Repo(dados).Lancamentos.Count);
        new AberturaSessao(dados, disparador, new RelogioFalso(Fabrica.Agora), () => Guid.Parse(IdFixo))
            .Nova("CHG-142", "Faturamento", Agente.Codex, null);
        Assert.Equal(1, vistos);
    }

    private sealed class Observador(Action aoIniciar) : IDisparador
    {
        public void Iniciar(ComandoTerminal comando) => aoIniciar();
    }

    [Fact]
    public void Falha_ao_iniciar_o_terminal_desfaz_o_lancamento()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        disparador.Falhar = true;
        Assert.False(abertura.Nova("CHG-142", "Faturamento", Agente.Codex, "ajuste").Ok);
        Assert.Empty(Repo(dados).Lancamentos);
    }

    [Fact]
    public void Claude_novo_nao_gera_lancamento()
    {
        var (abertura, dados, _, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.Nova("CHG-142", "Faturamento", Agente.Claude, null).Ok);
        Assert.Empty(Repo(dados).Lancamentos);
        Assert.Single(Repo(dados).Sessoes);
    }

    [Fact]
    public void Retomar_nao_gera_lancamento()
    {
        var (abertura, dados, _, pasta) = Novo();
        using var _ = pasta;
        var s = new SessaoRegistrada(Agente.Codex, IdFixo, "Faturamento", null, Fabrica.Agora, "CHG-142", "CHG-142");
        Assert.True(abertura.Retomar(s).Ok);
        Assert.Empty(Repo(dados).Lancamentos);
    }

    [Fact]
    public void Dois_cliques_geram_dois_lancamentos()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        var ids = new Queue<Guid>([Guid.Parse(ApoioVinculoAutomatico.L1), Guid.Parse(ApoioVinculoAutomatico.L2)]);
        var abertura = new AberturaSessao(dados, new DisparadorFalso(), new RelogioFalso(Fabrica.Agora), ids.Dequeue);
        abertura.Nova("CHG-142", "Faturamento", Agente.Codex, null);
        abertura.Nova("CHG-142", "Faturamento", Agente.Codex, null);
        Assert.Equal(2, Repo(dados).Lancamentos.Count);
    }
}
