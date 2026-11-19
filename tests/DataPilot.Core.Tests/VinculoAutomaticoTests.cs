using System.Text.Json.Nodes;
using DataPilot.Core;

namespace DataPilot.Core.Tests;

internal static class ApoioVinculoAutomatico
{
    internal const string L1 = "aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa";
    internal const string L2 = "bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb";
    internal const string L3 = "cccccccc-3333-4333-8333-cccccccccccc";
    internal const string S1 = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    internal const string S2 = "11111111-2222-3333-4444-555555555555";

    internal static readonly DateTimeOffset Base = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    internal static (DadosPainel Dados, PastaTemporaria Pasta) NovoDados(params string[] demandas)
    {
        var pasta = new PastaTemporaria();
        var dados = new DadosPainel(Path.Combine(pasta.Caminho, "demandas.json"));
        foreach (var d in demandas)
        {
            dados.CriarDemanda(d, d, null, Base);
            dados.VincularRepositorio(d, "Faturamento");
        }
        return (dados, pasta);
    }

    internal static Sessao Codex(string id, DateTimeOffset? criado, string cwd = @"C:\Fontes\Faturamento", Agente agente = Agente.Codex)
        => new(agente, id, cwd, StatusGravado.Executando, Base, criado);

    internal static Demanda Demanda(string id, bool arquivada, params LancamentoPendente[] lancamentos)
        => new(id, id, null, null, arquivada, [new RepositorioCadastrado("Faturamento", [], lancamentos)]);

    internal static LancamentoPendente Lanc(string id, DateTimeOffset criado, string? rotulo = null, Agente agente = Agente.Codex)
        => new(id, agente, criado, rotulo);
}

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
              {"id":"valido-nao","agente":"codex","criadoEm":"2026-11-14T10:00:00Z"},
              {"id":"{{ApoioVinculoAutomatico.L1}}","agente":"outro","criadoEm":"2026-11-14T10:00:00Z"},
              {"id":"{{ApoioVinculoAutomatico.L1}}","agente":"codex","criadoEm":"nao e data"},
              {"id":"{{ApoioVinculoAutomatico.L1}}","agente":"codex","criadoEm":"2026-11-14T10:00:00Z","rotulo":"a;b"},
              {"id":"{{ApoioVinculoAutomatico.L2}}","agente":"codex","criadoEm":"2026-11-14T10:00:00Z","rotulo":"ok"},
              {"id":"{{ApoioVinculoAutomatico.L2}}","agente":"codex","criadoEm":"2026-11-14T11:00:00Z"},
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
              "lancamentos":[{"id":"aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa","agente":"codex","criadoEm":"2026-11-14T10:00:00Z","desconhecido":"d"}]}]}]}
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

public class VinculoAutomaticoCorrespondenciaTests
{
    private static readonly DateTimeOffset B = ApoioVinculoAutomatico.Base;
    private static readonly DateTimeOffset Agora = B.AddHours(1);

    private static Demanda D(string id, bool arquivada, params LancamentoPendente[] l) => ApoioVinculoAutomatico.Demanda(id, arquivada, l);
    private static LancamentoPendente L(string id, DateTimeOffset criado, string? rotulo = null, Agente agente = Agente.Codex)
        => ApoioVinculoAutomatico.Lanc(id, criado, rotulo, agente);
    private static Sessao S(string id, DateTimeOffset? criado, string cwd = @"C:\Fontes\Faturamento", Agente agente = Agente.Codex)
        => ApoioVinculoAutomatico.Codex(id, criado, cwd, agente);

    private const string L1 = ApoioVinculoAutomatico.L1;
    private const string L2 = ApoioVinculoAutomatico.L2;
    private const string S1 = ApoioVinculoAutomatico.S1;
    private const string S2 = ApoioVinculoAutomatico.S2;

    [Fact]
    public void Sessao_criada_depois_do_lancamento_e_vinculada_com_demanda_repositorio_e_lancamento()
    {
        var v = Assert.Single(VinculoAutomatico.Calcular([D("CHG-142", false, L(L1, B, "ajuste"))], [S(S1, B.AddMinutes(5))], Agora));
        Assert.Equal(S1, v.Pedido.SessionId);
        Assert.Equal(Agente.Codex, v.Pedido.Agente);
        Assert.Equal("CHG-142", v.DemandaId);
        Assert.Equal("Faturamento", v.Pasta);
        Assert.Equal(L1, v.Lancamento.Id);
        Assert.Equal("ajuste", v.Lancamento.Rotulo);
    }

    [Fact]
    public void Criada_no_mesmo_instante_do_lancamento_corresponde()
        => Assert.Single(VinculoAutomatico.Calcular([D("A", false, L(L1, B))], [S(S1, B)], Agora));

    [Fact]
    public void Sessao_criada_antes_do_lancamento_nao_corresponde()
        => Assert.Empty(VinculoAutomatico.Calcular([D("A", false, L(L1, B))], [S(S1, B.AddSeconds(-1))], Agora));

    [Fact]
    public void Sem_data_de_criacao_do_arquivo_nao_corresponde()
        => Assert.Empty(VinculoAutomatico.Calcular([D("A", false, L(L1, B))], [S(S1, null)], Agora));

    [Fact]
    public void Agente_diferente_do_lancamento_nao_corresponde()
        => Assert.Empty(VinculoAutomatico.Calcular([D("A", false, L(L1, B))], [S(S1, B.AddMinutes(1), agente: Agente.Claude)], Agora));

    [Theory]
    [InlineData(@"C:\Fontes\Estoque")]
    [InlineData("")]
    [InlineData(@"C:\Fontes\Faturamento2")]
    public void Pasta_diferente_do_repositorio_nao_corresponde(string cwd)
        => Assert.Empty(VinculoAutomatico.Calcular([D("A", false, L(L1, B))], [S(S1, B.AddMinutes(1), cwd)], Agora));

    [Theory]
    [InlineData(@"c:\fontes\FATURAMENTO")]
    [InlineData(@"C:\Fontes\Faturamento\")]
    public void Pasta_igual_ao_repositorio_corresponde_sem_diferenciar_maiusculas(string cwd)
        => Assert.Single(VinculoAutomatico.Calcular([D("A", false, L(L1, B))], [S(S1, B.AddMinutes(1), cwd)], Agora));

    [Fact]
    public void Sessao_ja_registrada_nao_e_vinculada_de_novo()
    {
        var registrada = new SessaoRegistrada(Agente.Codex, S1, "Faturamento", null, B, "A");
        var demanda = new Demanda("A", "A", null, null, false, [new RepositorioCadastrado("Faturamento", [registrada], [L(L1, B)])]);
        Assert.Empty(VinculoAutomatico.Calcular([demanda], [S(S1, B.AddMinutes(1))], Agora));
    }

    [Fact]
    public void Identificador_fora_do_formato_guid_nao_corresponde()
        => Assert.Empty(VinculoAutomatico.Calcular([D("A", false, L(L1, B))], [S("sessao-1", B.AddMinutes(1))], Agora));

    [Fact]
    public void Lancamento_de_25_horas_nao_vincula_e_de_24_horas_vincula()
    {
        var sessoes = new[] { S(S1, B.AddHours(26)) };
        Assert.Empty(VinculoAutomatico.Calcular([D("A", false, L(L1, B))], sessoes, B.AddHours(25)));
        Assert.Single(VinculoAutomatico.Calcular([D("A", false, L(L1, B))], sessoes, B.AddHours(24)));
    }

    [Fact]
    public void Demanda_arquivada_nao_vincula()
        => Assert.Empty(VinculoAutomatico.Calcular([D("A", true, L(L1, B))], [S(S1, B.AddMinutes(1))], Agora));

    [Fact]
    public void Sem_lancamentos_nada_e_vinculado()
        => Assert.Empty(VinculoAutomatico.Calcular([D("A", false)], [S(S1, B.AddMinutes(1))], Agora));

    [Fact]
    public void Entre_varios_lancamentos_vale_o_mais_recente_anterior_a_sessao()
    {
        // Lançamento abandonado das 10:00 em A; clique das 10:30 em B; sessão criada às 10:35.
        var demandas = new[] { D("A", false, L(L1, B)), D("B", false, L(L2, B.AddMinutes(30))) };
        var v = Assert.Single(VinculoAutomatico.Calcular(demandas, [S(S1, B.AddMinutes(35))], Agora));
        Assert.Equal("B", v.DemandaId);
        Assert.Equal(L2, v.Lancamento.Id);
    }

    [Fact]
    public void Lancamento_posterior_a_sessao_nao_e_considerado()
    {
        var demandas = new[] { D("A", false, L(L1, B)), D("B", false, L(L2, B.AddMinutes(30))) };
        var v = Assert.Single(VinculoAutomatico.Calcular(demandas, [S(S1, B.AddMinutes(10))], Agora));
        Assert.Equal("A", v.DemandaId);
    }

    [Fact]
    public void Cada_lancamento_vincula_uma_unica_sessao()
    {
        var r = VinculoAutomatico.Calcular([D("A", false, L(L1, B))], [S(S1, B.AddMinutes(1)), S(S2, B.AddMinutes(2))], Agora);
        var v = Assert.Single(r);
        Assert.Equal(S1, v.Pedido.SessionId);
    }

    [Fact]
    public void Dois_lancamentos_no_mesmo_card_vinculam_duas_sessoes_da_mais_antiga_para_a_mais_nova()
    {
        var demandas = new[] { D("A", false, L(L1, B), L(L2, B.AddMinutes(10))) };
        // Entrada fora de ordem: o painel trata da mais antiga para a mais nova.
        var r = VinculoAutomatico.Calcular(demandas, [S(S2, B.AddMinutes(20)), S(S1, B.AddMinutes(12))], Agora);
        Assert.Equal(2, r.Count);
        Assert.Equal(S1, r[0].Pedido.SessionId);
        Assert.Equal(L2, r[0].Lancamento.Id);
        Assert.Equal(S2, r[1].Pedido.SessionId);
        Assert.Equal(L1, r[1].Lancamento.Id);
        Assert.All(r, x => Assert.Equal("A", x.DemandaId));
    }

    [Fact]
    public void Sessao_antiga_na_mesma_pasta_nao_captura_o_lancamento_de_uma_sessao_nova()
    {
        // S1 existia antes do clique; S2 nasceu depois.
        var r = VinculoAutomatico.Calcular([D("A", false, L(L1, B))], [S(S1, B.AddMinutes(-30)), S(S2, B.AddMinutes(5))], Agora);
        Assert.Equal(S2, Assert.Single(r).Pedido.SessionId);
    }
}

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
        => PedidoVinculo.Criar(new Sessao(agente, id, @"C:\Fontes\Outra", StatusGravado.Ativa, B))!;

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
        Assert.Equal(["agente", "sessionId", "rotulo", "criadaEm", "vinculadaEm"], no.Select(p => p.Key));
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
        Assert.True(dados.RemoverSessao(S1).Ok);

        var lida = ApoioVinculoAutomatico.Codex(S1, B.AddMinutes(1));
        Assert.Empty(VinculoAutomatico.Calcular(dados.Carregar().Demandas, [lida], B.AddHours(1)));
    }

    [Fact]
    public void Campos_desconhecidos_do_repositorio_sao_preservados()
    {
        var json = $$"""
            {"demandas":[{"id":"A","titulo":"A","repositorios":[{"pasta":"Faturamento","z":1,"sessoes":[],
              "lancamentos":[{"id":"{{L1}}","agente":"codex","criadoEm":"2026-11-14T10:00:00Z"}]}]}]}
            """;
        var (dados, pasta, caminho) = DadosPainelTests.Novo(json);
        using var _p = pasta;
        Assert.True(dados.VincularPorLancamento(Pedido(S1), "A", "Faturamento", L1, B).Ok);
        Assert.Equal(1, (int)JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!["repositorios"]![0]!["z"]!);
    }
}

public class VinculoAutomaticoAplicacaoTests
{
    private static readonly DateTimeOffset B = ApoioVinculoAutomatico.Base;
    private const string L1 = ApoioVinculoAutomatico.L1;
    private const string S1 = ApoioVinculoAutomatico.S1;

    [Fact]
    public void Duas_instancias_sobre_o_mesmo_arquivo_registram_a_sessao_uma_unica_vez()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, "ajuste", B);
        var outra = new DadosPainel(dados.Caminho);
        var relogio = new RelogioFalso(B.AddHours(1));
        var lidas = new[] { ApoioVinculoAutomatico.Codex(S1, B.AddMinutes(5)) };

        // As duas instâncias partem do mesmo retrato dos dados, como duas janelas lendo ao mesmo tempo.
        var estado = dados.Carregar();
        var a = VinculoAutomatico.Aplicar(dados, estado, lidas, relogio);
        var b = VinculoAutomatico.Aplicar(outra, estado, lidas, relogio);

        Assert.Equal(1, a);
        Assert.Equal(0, b);
        var repo = dados.Carregar().Demandas.Single().Repositorios.Single();
        var s = Assert.Single(repo.Sessoes);
        Assert.Equal(("ajuste", S1), (s.Rotulo, s.SessionId));
        Assert.Empty(repo.Lancamentos);
    }

    [Fact]
    public void Dados_indisponiveis_nao_gravam_nada()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, B);
        var estado = dados.Carregar();
        File.WriteAllText(dados.Caminho, "lixo");
        var n = VinculoAutomatico.Aplicar(dados, estado with { Disponivel = false }, [ApoioVinculoAutomatico.Codex(S1, B.AddMinutes(5))], new RelogioFalso(B.AddHours(1)));
        Assert.Equal(0, n);
        Assert.Equal("lixo", File.ReadAllText(dados.Caminho));
    }

    [Fact]
    public void Falha_de_gravacao_e_ignorada_e_tentada_na_leitura_seguinte()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, B);
        var estado = dados.Carregar();
        var relogio = new RelogioFalso(B.AddHours(1));
        var lidas = new[] { ApoioVinculoAutomatico.Codex(S1, B.AddMinutes(5)) };

        // Arquivo de dados bloqueado para troca: a gravação falha e nada é lançado ao chamador.
        var antes = File.ReadAllText(dados.Caminho);
        int durante;
        using (new FileStream(dados.Caminho, FileMode.Open, FileAccess.Read, FileShare.None))
            durante = VinculoAutomatico.Aplicar(dados, estado, lidas, relogio);
        Assert.Equal(0, durante);
        Assert.Equal(antes, File.ReadAllText(dados.Caminho));

        Assert.Equal(1, VinculoAutomatico.Aplicar(dados, estado, lidas, relogio));
    }

    [Fact]
    public void Sem_correspondencia_o_arquivo_permanece_inalterado()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, B);
        var antes = File.ReadAllBytes(dados.Caminho);
        var n = VinculoAutomatico.Aplicar(dados, dados.Carregar(),
            [ApoioVinculoAutomatico.Codex(S1, B.AddMinutes(-5)), ApoioVinculoAutomatico.Codex("22222222-3333-4444-5555-666666666666", B.AddMinutes(5), @"C:\Fontes\Estoque")],
            new RelogioFalso(B.AddHours(1)));
        Assert.Equal(0, n);
        Assert.Equal(antes, File.ReadAllBytes(dados.Caminho));
    }
}
