using System.Text.Json.Nodes;
using DataPilot.Core.Models;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Persistence;

public class LancamentosRegistroTests
{
    private static JsonObject Raiz(string caminho) => (JsonObject)JsonNode.Parse(File.ReadAllText(caminho))!;

    [Fact]
    public void Registrar_grava_id_agente_rotulo_e_data()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        Assert.True(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, "ajuste", ApoioVinculoAutomatico.Base).Ok);

        var l = Assert.Single(dados.Carregar().Demandas.Single().Repositorios.Single().Lancamentos);
        Assert.Equal(new LancamentoPendente(ApoioVinculoAutomatico.L1, Agente.Codex, ApoioVinculoAutomatico.Base, "ajuste"), l);
    }

    [Fact]
    public void Registrar_sem_rotulo_nao_grava_rotulo()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, null, ApoioVinculoAutomatico.Base);
        var no = Raiz(dados.Caminho)["demandas"]![0]!["repositorios"]![0]!["lancamentos"]![0]!.AsObject();
        Assert.False(no.ContainsKey("rotulo"));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("aaaaaaaa1111411181 11aaaaaaaaaaaa")]
    public void Registrar_com_id_invalido_e_recusado(string id)
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        Assert.False(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, id, null, ApoioVinculoAutomatico.Base).Ok);
        Assert.Empty(dados.Carregar().Demandas.Single().Repositorios.Single().Lancamentos);
    }

    [Fact]
    public void Registrar_com_rotulo_invalido_ou_repositorio_inexistente_e_recusado()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        Assert.False(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, "a;b", ApoioVinculoAutomatico.Base).Ok);
        Assert.False(dados.RegistrarLancamento("CHG-142", "Estoque", Agente.Codex, ApoioVinculoAutomatico.L1, null, ApoioVinculoAutomatico.Base).Ok);
        Assert.False(dados.RegistrarLancamento("Z", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, null, ApoioVinculoAutomatico.Base).Ok);
    }

    [Fact]
    public void Registrar_o_mesmo_id_duas_vezes_e_recusado()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        Assert.True(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, null, ApoioVinculoAutomatico.Base).Ok);
        Assert.False(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, null, ApoioVinculoAutomatico.Base).Ok);
    }

    [Fact]
    public void Desfazer_remove_so_o_lancamento_indicado()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, null, ApoioVinculoAutomatico.Base);
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L2, null, ApoioVinculoAutomatico.Base);
        Assert.True(dados.DesfazerLancamento(ApoioVinculoAutomatico.L1).Ok);
        Assert.Equal(ApoioVinculoAutomatico.L2, Assert.Single(dados.Carregar().Demandas.Single().Repositorios.Single().Lancamentos).Id);
        Assert.False(dados.DesfazerLancamento(ApoioVinculoAutomatico.L1).Ok);
    }

    [Fact]
    public void Lancamento_vencido_e_removido_ao_gravar_outro_no_mesmo_repositorio()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        var b = ApoioVinculoAutomatico.Base;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, null, b);
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L2, null, b.AddHours(24));
        // Exatamente 24 horas ainda vale.
        Assert.Equal(2, dados.Carregar().Demandas.Single().Repositorios.Single().Lancamentos.Count);

        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L3, null, b.AddHours(25));
        var ids = dados.Carregar().Demandas.Single().Repositorios.Single().Lancamentos.Select(l => l.Id).ToList();
        Assert.Equal([ApoioVinculoAutomatico.L2, ApoioVinculoAutomatico.L3], ids);
    }

    [Fact]
    public void Poda_nao_toca_outros_repositorios()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.VincularRepositorio("CHG-142", "Estoque");
        var b = ApoioVinculoAutomatico.Base;
        dados.RegistrarLancamento("CHG-142", "Estoque", Agente.Codex, ApoioVinculoAutomatico.L1, null, b);
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L2, null, b.AddHours(30));
        var estoque = dados.Carregar().Demandas.Single().Repositorios.Single(r => r.Pasta == "Estoque");
        Assert.Single(estoque.Lancamentos);
    }

    [Fact]
    public void Arquivo_sem_lancamentos_e_carregado()
    {
        var (dados, pasta, _) = DadosPainelTests.Novo(DadosPainelTests.ArquivoDaEtapa3());
        using var _p = pasta;
        var estado = dados.Carregar();
        Assert.True(estado.Disponivel);
        Assert.All(estado.Demandas.Single().Repositorios, r => Assert.Empty(r.Lancamentos));
    }

    [Fact]
    public void Lancamentos_invalidos_editados_a_mao_sao_desconsiderados()
    {
        var json = $$"""
            {"demandas":[{"id":"A","titulo":"A","repositorios":[{"pasta":"Faturamento","sessoes":[],"lancamentos":[
              {"id":"valido-nao","agente":"codex","execucao":"container","criadoEm":"2026-11-14T10:00:00Z"},
              {"id":"{{ApoioVinculoAutomatico.L1}}","agente":"outro","execucao":"container","criadoEm":"2026-11-14T10:00:00Z"},
              {"id":"{{ApoioVinculoAutomatico.L1}}","agente":"codex","execucao":"container","criadoEm":"nao e data"},
              {"id":"{{ApoioVinculoAutomatico.L1}}","agente":"codex","execucao":"container","criadoEm":"2026-11-14T10:00:00Z","rotulo":"a;b"},
              {"id":"{{ApoioVinculoAutomatico.L2}}","agente":"codex","execucao":"container","criadoEm":"2026-11-14T10:00:00Z","rotulo":"ok"},
              {"id":"{{ApoioVinculoAutomatico.L2}}","agente":"codex","execucao":"container","criadoEm":"2026-11-14T11:00:00Z"},
              "texto", 7]}]}]}
            """;
        var (dados, pasta, _) = DadosPainelTests.Novo(json);
        using var _p = pasta;
        var l = Assert.Single(dados.Carregar().Demandas.Single().Repositorios.Single().Lancamentos);
        Assert.Equal(ApoioVinculoAutomatico.L2, l.Id);
        Assert.Equal("ok", l.Rotulo);
    }

    [Fact]
    public void Lista_de_lancamentos_que_nao_e_lista_torna_o_arquivo_ilegivel_e_nao_e_sobrescrita()
    {
        var json = """{"demandas":[{"id":"A","titulo":"A","repositorios":[{"pasta":"Faturamento","lancamentos":5}]}]}""";
        var (dados, pasta, caminho) = DadosPainelTests.Novo(json);
        using var _p = pasta;
        Assert.False(dados.Carregar().Disponivel);
        Assert.False(dados.RegistrarLancamento("A", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L1, null, ApoioVinculoAutomatico.Base).Ok);
        Assert.Equal(json, File.ReadAllText(caminho));
    }

    [Fact]
    public void Campos_desconhecidos_sao_preservados_ao_gravar_lancamentos()
    {
        var json = """
            {"extra":1,"demandas":[{"id":"A","titulo":"A","x":"y","repositorios":[{"pasta":"Faturamento","z":true,"sessoes":[],
              "lancamentos":[{"id":"aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa","agente":"codex","execucao":"container","criadoEm":"2026-11-14T10:00:00Z","desconhecido":"d"}]}]}]}
            """;
        var (dados, pasta, caminho) = DadosPainelTests.Novo(json);
        using var _p = pasta;
        Assert.True(dados.RegistrarLancamento("A", "Faturamento", Agente.Codex, ApoioVinculoAutomatico.L2, null, ApoioVinculoAutomatico.Base).Ok);

        var raiz = Raiz(caminho);
        Assert.Equal(1, (int)raiz["extra"]!);
        var repo = raiz["demandas"]![0]!["repositorios"]![0]!;
        Assert.Equal("y", (string)raiz["demandas"]![0]!["x"]!);
        Assert.True((bool)repo["z"]!);
        Assert.Equal("d", (string)repo["lancamentos"]![0]!["desconhecido"]!);
        Assert.Equal(2, repo["lancamentos"]!.AsArray().Count);
    }
}
