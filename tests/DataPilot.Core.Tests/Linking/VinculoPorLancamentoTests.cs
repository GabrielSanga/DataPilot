using System.Text.Json.Nodes;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Tests.Helpers;
using DataPilot.Core.Tests.Persistence;

namespace DataPilot.Core.Tests.Linking;

public class VinculoPorLancamentoTests
{
    private static readonly DateTimeOffset B = ApoioVinculoAutomatico.Base;
    private const string L1 = ApoioVinculoAutomatico.L1;
    private const string S1 = ApoioVinculoAutomatico.S1;
    private const string S2 = ApoioVinculoAutomatico.S2;

    private static (DadosPainel Dados, PastaTemporaria Pasta) ComLancamento(string? rotulo = "ajuste", params string[] demandas)
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados(demandas.Length == 0 ? ["CHG-142"] : demandas);
        dados.RegistrarLancamento(demandas.Length == 0 ? "CHG-142" : demandas[0], "Faturamento", Agente.Codex, L1, rotulo, B);
        return (dados, pasta);
    }

    private static PedidoVinculo Pedido(string id, Agente agente = Agente.Codex)
        => PedidoVinculo.Criar(new Sessao(agente, id, "/workspace/Outra", StatusGravado.Ativa, B))!;

    private static RepositorioCadastrado Repo(DadosPainel d, string demanda = "CHG-142")
        => d.Carregar().Demandas.Single(x => x.Id == demanda).Repositorios.Single();

    [Fact]
    public void Vincula_registra_a_sessao_com_rotulo_e_remove_o_lancamento_na_mesma_gravacao()
    {
        var (dados, pasta) = ComLancamento();
        using var _ = pasta;
        var quando = B.AddMinutes(7);
        Assert.True(dados.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, quando).Ok);

        var repo = Repo(dados);
        Assert.Empty(repo.Lancamentos);
        var s = Assert.Single(repo.Sessoes);
        Assert.Equal(new SessaoRegistrada(Agente.Codex, S1, "Faturamento", "ajuste", quando, "CHG-142", "CHG-142"), s);
    }

    [Fact]
    public void Registro_tem_vinculadaEm_e_so_agente_identificador_rotulo_e_datas_do_painel()
    {
        var (dados, pasta) = ComLancamento();
        using var _ = pasta;
        var quando = B.AddMinutes(7);
        dados.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, quando);

        var no = ((JsonObject)JsonNode.Parse(File.ReadAllText(dados.Caminho))!)["demandas"]![0]!["repositorios"]![0]!["sessoes"]![0]!.AsObject();
        Assert.Equal(["agente", "sessionId", "execucao", "rotulo", "criadaEm", "vinculadaEm"], no.Select(p => p.Key));
        Assert.Equal("codex", (string)no["agente"]!);
        Assert.Equal(quando, DateTimeOffset.Parse((string)no["vinculadaEm"]!));
        Assert.DoesNotContain("Outra", File.ReadAllText(dados.Caminho));
    }

    [Fact]
    public void Sem_rotulo_no_lancamento_a_sessao_fica_sem_rotulo()
    {
        var (dados, pasta) = ComLancamento(rotulo: null);
        using var _ = pasta;
        dados.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, B);
        Assert.Null(Repo(dados).Sessoes.Single().Rotulo);
    }

    [Fact]
    public void Lancamento_que_nao_existe_mais_e_recusado_sem_alterar_o_arquivo()
    {
        var (dados, pasta) = ComLancamento();
        using var _ = pasta;
        dados.DesfazerLancamento(L1);
        var antes = File.ReadAllText(dados.Caminho);
        var r = dados.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, B);
        Assert.False(r.Ok);
        Assert.Equal(antes, File.ReadAllText(dados.Caminho));
    }

    [Fact]
    public void Segunda_aplicacao_do_mesmo_vinculo_e_recusada_e_a_sessao_fica_uma_vez()
    {
        var (dados, pasta) = ComLancamento();
        using var _ = pasta;
        var outra = new DadosPainel(dados.Caminho);
        Assert.True(dados.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, B).Ok);
        Assert.False(outra.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, B).Ok);
        Assert.Single(Repo(dados).Sessoes);
    }

    [Fact]
    public void Identificador_ja_registrado_e_recusado_e_o_lancamento_continua()
    {
        var (dados, pasta) = ComLancamento();
        using var _ = pasta;
        dados.VincularRepositorio("CHG-142", "Estoque");
        dados.RegistrarSessao("CHG-142", "Estoque", Agente.Codex, S1, null, B);
        Assert.False(dados.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, B).Ok);
        var fat = dados.Carregar().Demandas.Single().Repositorios.Single(r => r.Pasta == "Faturamento");
        Assert.Single(fat.Lancamentos);
        Assert.Empty(fat.Sessoes);
    }

    [Fact]
    public void Agente_diferente_do_lancamento_e_recusado()
    {
        var (dados, pasta) = ComLancamento();
        using var _ = pasta;
        Assert.False(dados.VincularPorLancamento(Pedido(S1, Agente.Claude), "CHG-142", "Faturamento", L1, B).Ok);
        Assert.Single(Repo(dados).Lancamentos);
        Assert.Empty(Repo(dados).Sessoes);
    }

    [Fact]
    public void Demanda_arquivada_e_recusada()
    {
        var (dados, pasta) = ComLancamento();
        using var _ = pasta;
        dados.ArquivarDemanda("CHG-142");
        Assert.False(dados.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, B).Ok);
        Assert.Single(Repo(dados).Lancamentos);
    }

    [Fact]
    public void Lancamento_de_outra_demanda_nao_e_usado()
    {
        var (dados, pasta) = ComLancamento("x", "A", "B");
        using var _ = pasta;
        Assert.False(dados.VincularPorLancamento(Pedido(S1), "B", "Faturamento", L1, B).Ok);
        Assert.Single(Repo(dados, "A").Lancamentos);
    }

    [Fact]
    public void Arquivo_ilegivel_nao_e_sobrescrito()
    {
        var (dados, pasta) = ComLancamento();
        using var _ = pasta;
        File.WriteAllText(dados.Caminho, "lixo");
        Assert.False(dados.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, B).Ok);
        Assert.Equal("lixo", File.ReadAllText(dados.Caminho));
    }

    [Fact]
    public void Removido_o_registro_a_sessao_nao_e_vinculada_de_novo()
    {
        var (dados, pasta) = ComLancamento();
        using var _ = pasta;
        dados.VincularPorLancamento(Pedido(S1), "CHG-142", "Faturamento", L1, B);
        Assert.True(dados.DesfazerRegistro(S1).Ok);

        var lida = ApoioVinculoAutomatico.Codex(S1, B.AddMinutes(1));
        Assert.Empty(VinculoAutomatico.Calcular(dados.Carregar().Demandas, [lida], B.AddHours(1)));
    }

    [Fact]
    public void Campos_desconhecidos_do_repositorio_sao_preservados()
    {
        var json = $$"""
            {"demandas":[{"id":"A","titulo":"A","repositorios":[{"pasta":"Faturamento","z":1,"sessoes":[],
              "lancamentos":[{"id":"{{L1}}","agente":"codex","execucao":"container","criadoEm":"2026-11-14T10:00:00Z"}]}]}]}
            """;
        var (dados, pasta, caminho) = DadosPainelTests.Novo(json);
        using var _p = pasta;
        Assert.True(dados.VincularPorLancamento(Pedido(S1), "A", "Faturamento", L1, B).Ok);
        Assert.Equal(1, (int)JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!["repositorios"]![0]!["z"]!);
    }
}
