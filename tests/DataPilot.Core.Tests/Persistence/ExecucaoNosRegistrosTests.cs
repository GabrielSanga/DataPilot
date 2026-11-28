using System.Text.Json.Nodes;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Persistence;

/// <summary>Cenários de "Ambiente de execução nos registros" (dados-do-painel).</summary>
public class ExecucaoNosRegistrosTests
{
    private const string Id = DadosPainelTests.Id1;
    private const string L1 = DadosPainelTests.Id2;
    private const string Outro = "11111111-2222-3333-4444-555555555555";

    private static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) Com(string sessao, string lancamento)
        => DadosPainelTests.Novo($$"""
            {"demandas":[{"id":"CHG-142","titulo":"Ajuste","arquivada":false,"repositorios":[
              {"pasta":"Faturamento",
               "sessoes":[{"agente":"claude","sessionId":"{{Id}}"{{sessao}}}],
               "lancamentos":[{"id":"{{L1}}","agente":"codex","criadoEm":"2026-11-14T12:00:00+00:00"{{lancamento}}}]}]}]}
            """);

    private static RepositorioCadastrado Repo(DadosPainel d) => d.Carregar().Demandas.Single(x => x.Id == "CHG-142").Repositorios.Single();

    private static PedidoVinculo Pedido(string id, Agente agente = Agente.Codex)
        => PedidoVinculo.Criar(Fabrica.Sessao(StatusGravado.Ativa, TimeSpan.Zero, id, agente))!;

    private static JsonObject NoRepo(string caminho)
        => JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!["repositorios"]![0]!.AsObject();

    [Fact]
    public void Registros_do_container_sao_lidos()
    {
        var (dados, pasta, _) = Com(",\"execucao\":\"container\"", ",\"execucao\":\"container\"");
        using var _ = pasta;
        Assert.Single(Repo(dados).Sessoes);
        Assert.Single(Repo(dados).Lancamentos);
    }

    [Theory]
    [InlineData("")]
    [InlineData(",\"execucao\":\"windows\"")]
    [InlineData(",\"execucao\":\"wsl\"")]
    [InlineData(",\"execucao\":7")]
    public void Registro_que_nao_e_do_container_e_desconsiderado_e_nao_vincula(string campo)
    {
        var (dados, pasta, _) = Com(campo, campo);
        using var _ = pasta;
        Assert.Empty(Repo(dados).Sessoes);
        Assert.Empty(Repo(dados).Lancamentos);
        Assert.False(dados.VincularPorLancamento(Pedido(Outro), "CHG-142", "Faturamento", L1, Fabrica.Agora).Ok);
    }

    [Theory]
    [InlineData("")]
    [InlineData(",\"execucao\":\"windows\"")]
    public void Registro_desconsiderado_e_preservado_numa_gravacao_do_mesmo_repositorio(string campo)
    {
        var (dados, pasta, caminho) = Com(campo, campo);
        using var _ = pasta;
        var antes = NoRepo(caminho);

        Assert.True(dados.RegistrarSessao("CHG-142", "Faturamento", Agente.Claude, Outro, null, Fabrica.Agora).Ok);
        // Muito depois da validade: a limpeza de lançamentos vencidos não toca no registro desconsiderado.
        Assert.True(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, null,
            Fabrica.Agora.AddDays(30)).Ok);

        var depois = NoRepo(caminho);
        Assert.True(JsonNode.DeepEquals(antes["sessoes"]![0], depois["sessoes"]![0]));
        Assert.True(JsonNode.DeepEquals(antes["lancamentos"]![0], depois["lancamentos"]![0]));
    }

    [Fact]
    public void Grava_container_na_sessao_e_no_lancamento()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        Assert.True(dados.RegistrarSessao("CHG-142", "Faturamento", Agente.Claude, Id, null, Fabrica.Agora).Ok);
        Assert.True(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, Fabrica.Agora).Ok);

        var repo = NoRepo(dados.Caminho);
        Assert.Equal("container", (string?)repo["sessoes"]![0]!["execucao"]);
        Assert.Equal("container", (string?)repo["lancamentos"]![0]!["execucao"]);
    }

    [Fact]
    public void Vinculo_por_lancamento_grava_container()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, Fabrica.Agora);

        Assert.True(dados.VincularPorLancamento(Pedido(Id), "CHG-142", "Faturamento", L1, Fabrica.Agora).Ok);

        Assert.Equal("container", (string?)NoRepo(dados.Caminho)["sessoes"]![0]!["execucao"]);
        Assert.Single(Repo(dados).Sessoes);
    }

    [Fact]
    public void Vinculo_manual_grava_container()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;

        Assert.True(dados.VincularSessao(Pedido(Id), "CHG-142", "Faturamento", null, Fabrica.Agora).Ok);

        Assert.Single(Repo(dados).Sessoes);
    }
}
