using System.Text.Json.Nodes;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Persistence;

public class ExcluirSessaoTests
{
    private const string Id1 = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    private const string IdHook = "abc_DEF-123";

    private static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) ComSessao()
    {
        var r = DadosPainelTests.Novo();
        r.Dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        r.Dados.VincularRepositorio("A", "X");
        r.Dados.RegistrarSessao("A", "X", Agente.Claude, Id1, null, Fabrica.Agora);
        return r;
    }

    [Fact]
    public void Exclusao_remove_o_registro_e_acrescenta_a_lista_na_mesma_gravacao()
    {
        var (dados, pasta, caminho) = ComSessao();
        using var _p = pasta;
        Assert.True(dados.ExcluirSessao(Agente.Claude, Id1, Fabrica.Agora).Ok);

        var raiz = JsonNode.Parse(File.ReadAllText(caminho))!;
        Assert.Empty(raiz["demandas"]![0]!["repositorios"]![0]!["sessoes"]!.AsArray());
        var item = Assert.Single(raiz["excluidas"]!.AsArray())!;
        Assert.Equal("claude", (string?)item["agente"]);
        Assert.Equal(Id1, (string?)item["sessionId"]);
        Assert.Equal(Fabrica.Agora, DateTimeOffset.Parse((string)item["excluidaEm"]!));

        var estado = dados.Carregar();
        Assert.Equal(new SessaoExcluida(Agente.Claude, Id1, Fabrica.Agora), Assert.Single(estado.Excluidas));
    }

    [Fact]
    public void Sessao_nao_registrada_tambem_pode_ser_excluida_com_a_regra_do_hook()
    {
        var (dados, pasta, _) = DadosPainelTests.Novo();
        using var _p = pasta;
        Assert.True(dados.ExcluirSessao(Agente.Codex, IdHook, Fabrica.Agora).Ok);
        Assert.Equal(IdHook, Assert.Single(dados.Carregar().Excluidas).SessionId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a b")]
    [InlineData("a;b")]
    [InlineData("a/b")]
    public void Identificador_fora_da_regra_do_hook_e_recusado_sem_alterar(string id)
    {
        var (dados, pasta, caminho) = ComSessao();
        using var _p = pasta;
        var antes = File.ReadAllText(caminho);
        Assert.False(dados.ExcluirSessao(Agente.Claude, id, Fabrica.Agora).Ok);
        Assert.False(dados.ExcluirSessao(Agente.Claude, new string('a', 101), Fabrica.Agora).Ok);
        Assert.Equal(antes, File.ReadAllText(caminho));
    }

    [Fact]
    public void Exclusao_repetida_nao_duplica_o_item()
    {
        var (dados, pasta, _) = ComSessao();
        using var _p = pasta;
        dados.ExcluirSessao(Agente.Claude, Id1, Fabrica.Agora);
        Assert.True(dados.ExcluirSessao(Agente.Claude, Id1.ToUpperInvariant(), Fabrica.Agora.AddHours(1)).Ok);
        Assert.Single(dados.Carregar().Excluidas);
    }

    [Fact]
    public void Itens_invalidos_da_lista_sao_desconsiderados_na_carga()
    {
        var (dados, pasta, _) = DadosPainelTests.Novo($$"""
            {"demandas":[],"excluidas":[
              {"agente":"claude","sessionId":"{{Id1}}","excluidaEm":"2026-11-14T10:00:00Z"},
              {"agente":"outro","sessionId":"x"},
              {"agente":"codex","sessionId":"a;b"},
              {"agente":"codex"},
              5]}
            """);
        using var _p = pasta;
        Assert.Equal([Id1], dados.Carregar().Excluidas.Select(e => e.SessionId));
    }

    [Fact]
    public void Arquivo_sem_a_lista_e_carregado_normalmente()
    {
        var (dados, pasta, _) = DadosPainelTests.Novo(DadosPainelTests.ArquivoDaEtapa3());
        using var _p = pasta;
        var estado = dados.Carregar();
        Assert.True(estado.Disponivel);
        Assert.Empty(estado.Excluidas);
    }

    [Fact]
    public void Excluidas_que_nao_e_lista_torna_o_arquivo_ilegivel()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo("""{"demandas":[],"excluidas":5}""");
        using var _p = pasta;
        Assert.False(dados.Carregar().Disponivel);
        Assert.False(dados.ExcluirSessao(Agente.Claude, Id1, Fabrica.Agora).Ok);
        Assert.Equal("""{"demandas":[],"excluidas":5}""", File.ReadAllText(caminho));
    }

    [Fact]
    public void Excluir_preserva_o_resto_do_arquivo()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo($$"""
            {"versao":2,"demandas":[{"id":"A","titulo":"A","extra":1,"repositorios":[{"pasta":"X","sessoes":[{"agente":"claude","sessionId":"{{Id1}}"}]}]}]}
            """);
        using var _p = pasta;
        dados.ExcluirSessao(Agente.Claude, Id1, Fabrica.Agora);
        var raiz = JsonNode.Parse(File.ReadAllText(caminho))!;
        Assert.Equal(2, (int?)raiz["versao"]);
        Assert.Equal(1, (int?)raiz["demandas"]![0]!["extra"]);
    }
}
