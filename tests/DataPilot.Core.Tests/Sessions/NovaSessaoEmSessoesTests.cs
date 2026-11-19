using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Sessions;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Sessions;

public class NovaSessaoEmSessoesTests
{
    private const string IdFixo = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    private const string IdOutro = "11111111-2222-3333-4444-555555555555";

    private static (AberturaSessao Abertura, DadosPainel Dados, DisparadorFalso Disparador, PastaTemporaria Pasta) Novo()
    {
        var pasta = new PastaTemporaria();
        var dados = new DadosPainel(Path.Combine(pasta.Caminho, "demandas.json"));
        var disparador = new DisparadorFalso();
        var abertura = new AberturaSessao(dados, disparador, new RelogioFalso(Fabrica.Agora), () => Guid.Parse(IdFixo));
        return (abertura, dados, disparador, pasta);
    }

    [Fact]
    public void Vincula_a_pasta_a_sem_demanda_e_registra_a_sessao_do_claude()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.NovaEmSessoes("Estoque", Agente.Claude, "teste").Ok);

        var d = Assert.Single(dados.Carregar().Demandas);
        Assert.Equal("sem-demanda", d.Id);
        var repo = Assert.Single(d.Repositorios);
        Assert.Equal("Estoque", repo.Pasta);
        Assert.Equal(IdFixo, Assert.Single(repo.Sessoes).SessionId);
        var comando = Assert.Single(disparador.Comandos);
        Assert.Equal("Claude · Estoque · teste", comando.Argumentos[4]);
    }

    [Fact]
    public void Pasta_ja_vinculada_e_reaproveitada_e_codex_gera_lancamento()
    {
        var (abertura, dados, _, pasta) = Novo();
        using var _ = pasta;
        Assert.True(abertura.NovaEmSessoes("Estoque", Agente.Codex, null).Ok);
        var repo = Assert.Single(dados.Carregar().Demandas.Single().Repositorios);
        Assert.Single(repo.Lancamentos);
    }

    [Fact]
    public void Pasta_invalida_e_recusada_sem_gravar()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        Assert.False(abertura.NovaEmSessoes("a;b", Agente.Claude, null).Ok);
        Assert.Empty(dados.Carregar().Demandas);
        Assert.Empty(disparador.Comandos);
    }

    [Fact]
    public void Retomar_nao_registrada_registra_em_sessoes_e_abre_a_conversa()
    {
        var (abertura, dados, disparador, pasta) = Novo();
        using var _ = pasta;
        var pedido = PedidoVinculo.Criar(new Sessao(Agente.Codex, IdOutro, @"C:\Fontes\Faturamento", StatusGravado.Ativa, Fabrica.Agora))!;
        Assert.True(abertura.RetomarNaoRegistrada(pedido, "Faturamento").Ok);

        var s = dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes.Single();
        Assert.Equal(IdOutro, s.SessionId);
        var args = Assert.Single(disparador.Comandos).Argumentos;
        Assert.Equal("Codex · Faturamento", args[4]);
        Assert.Equal(["-Repo", "Faturamento", "-Sessao", IdOutro, "-Modo", "retomar"], args.TakeLast(6));
    }

    [Fact]
    public void Retomar_nao_registrada_desfaz_o_registro_se_o_terminal_nao_abre()
    {
        var (_, dados, _, pasta) = Novo();
        using var _p = pasta;
        var abertura = new AberturaSessao(dados, new DisparadorFalso { Falhar = true }, new RelogioFalso(Fabrica.Agora));
        var pedido = PedidoVinculo.Criar(new Sessao(Agente.Claude, IdOutro, @"C:\Fontes\F", StatusGravado.Ativa, Fabrica.Agora))!;
        Assert.False(abertura.RetomarNaoRegistrada(pedido, "F").Ok);
        Assert.Empty(dados.Carregar().Demandas.SelectMany(d => d.Repositorios).SelectMany(r => r.Sessoes));
    }
}
