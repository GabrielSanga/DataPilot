using DataPilot.Core.Display;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Persistence;

public class CancelarLancamentoTests
{
    private const string L1 = ApoioVinculoAutomatico.L1;
    private const string L2 = ApoioVinculoAutomatico.L2;
    private const string S1 = ApoioVinculoAutomatico.S1;
    private static readonly DateTimeOffset Base = ApoioVinculoAutomatico.Base;

    private static (DadosPainel Dados, PastaTemporaria Pasta) ComDoisLancamentos()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, "um", Base);
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L2, "dois", Base.AddMinutes(1));
        return (dados, pasta);
    }

    [Fact]
    public void Cancelar_remove_o_lancamento_do_arquivo_de_dados()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, Base);

        Assert.True(dados.CancelarLancamento(L1).Ok);
        Assert.Empty(dados.Carregar().Demandas.Single().Repositorios.Single().Lancamentos);
    }

    [Fact]
    public void Cancelar_um_de_dois_deixa_o_outro_pendente_e_no_card()
    {
        var (dados, pasta) = ComDoisLancamentos();
        using var _ = pasta;

        Assert.True(dados.CancelarLancamento(L1).Ok);

        var estado = dados.Carregar();
        Assert.Equal(L2, Assert.Single(estado.Demandas.Single().Repositorios.Single().Lancamentos).Id);
        var v = VisaoAgrupada.Montar(estado.Demandas, [], false, false, new RelogioFalso(Base.AddMinutes(5)));
        Assert.Equal(L2, Assert.Single(v.Demandas.Single().Cards.Single().Lancamentos).Lancamento.Id);
    }

    [Fact]
    public void Cancelar_lancamento_que_nao_existe_nao_altera_o_arquivo()
    {
        var (dados, pasta) = ComDoisLancamentos();
        using var _ = pasta;
        var antes = File.ReadAllBytes(dados.Caminho);
        var escrita = File.GetLastWriteTimeUtc(dados.Caminho);

        var r = dados.CancelarLancamento("dddddddd-4444-4444-8444-dddddddddddd");

        Assert.False(r.Ok);
        Assert.Equal(antes, File.ReadAllBytes(dados.Caminho));
        Assert.Equal(escrita, File.GetLastWriteTimeUtc(dados.Caminho));
    }

    [Fact]
    public void Cancelar_lancamento_ja_usado_por_outra_janela_nao_altera_a_sessao_vinculada()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, "ajuste", Base);
        Assert.True(dados.VincularPorLancamento(PedidoVinculo.Criar(ApoioVinculoAutomatico.Codex(S1, Base))!, "CHG-142", "Faturamento", L1, Base.AddMinutes(1)).Ok);
        var antes = File.ReadAllBytes(dados.Caminho);

        Assert.False(dados.CancelarLancamento(L1).Ok);

        Assert.Equal(antes, File.ReadAllBytes(dados.Caminho));
        Assert.Equal(S1, dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes.Single().SessionId);
    }

    [Fact]
    public void Sessao_que_surge_depois_do_cancelamento_nao_e_vinculada()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, Base);
        Assert.True(dados.CancelarLancamento(L1).Ok);

        var sessao = ApoioVinculoAutomatico.Codex(S1, Base.AddMinutes(2));
        var relogio = new RelogioFalso(Base.AddMinutes(3));
        var estado = dados.Carregar();
        var gravados = VinculoAutomatico.Aplicar(dados, estado, [sessao], relogio);

        Assert.Equal(0, gravados);
        Assert.Empty(dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes);
        var v = VisaoAgrupada.Montar(dados.Carregar().Demandas, [sessao], false, false, relogio);
        Assert.Equal(S1, Assert.Single(v.Sessoes.NaoRegistradas).Sessao.SessionId);
    }
}
