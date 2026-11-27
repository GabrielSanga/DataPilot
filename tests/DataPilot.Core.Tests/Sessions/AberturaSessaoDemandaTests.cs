using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Sessions;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Sessions;

public class AberturaSessaoDemandaTests
{
    private const string IdFixo = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";

    private static (AberturaSessao Abertura, DadosPainel Dados, DisparadorFalso Disparador, PastaTemporaria Pasta) Novo()
    {
        var pasta = new PastaTemporaria();
        var dados = new DadosPainel(Path.Combine(pasta.Caminho, "demandas.json"));
        dados.SalvarConfiguracoes(Fabrica.Windows);
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
    public void Retomar_sessao_vinculada_a_um_trabalho_abre_no_mesmo_repositorio()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.NovaEmSessoes("Faturamento", Agente.Claude, null).Ok);
        dados.CriarDemanda("C", "Meu trabalho", null, Fabrica.Agora);
        var pedido = PedidoVinculo.Criar(new Sessao(Agente.Claude, IdFixo, @"C:\Fontes\Faturamento", StatusGravado.Ativa, Fabrica.Agora))!;
        Assert.True(dados.VincularSessao(pedido, "C", "Faturamento", null, Fabrica.Agora).Ok);

        var sessao = dados.Carregar().Demandas.Single(d => d.Id == "C").Repositorios.Single().Sessoes.Single();
        Assert.True(abertura.Retomar(sessao).Ok);
        var args = disparador.Comandos.Last().Argumentos;
        Assert.Equal("Meu trabalho · Faturamento", args[4]);
        Assert.Equal(["-Repo", "Faturamento", "-Sessao", IdFixo, "-Modo", "retomar"], args.TakeLast(6));
    }
}
