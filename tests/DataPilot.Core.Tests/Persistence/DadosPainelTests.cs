using System.Text.Json.Nodes;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Persistence;

public class DadosPainelTests
{
    internal const string Id1 = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    internal const string Id2 = "11111111-2222-3333-4444-555555555555";
    internal const string Id3 = "22222222-3333-4444-5555-666666666666";

    internal static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) Novo(string? conteudo = null)
    {
        var pasta = new PastaTemporaria();
        var caminho = Path.Combine(pasta.Caminho, "Painel", "demandas.json");
        if (conteudo is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
            File.WriteAllText(caminho, conteudo);
        }
        return (new DadosPainel(caminho), pasta, caminho);
    }

    /// <summary>Arquivo no formato gravado pela Etapa 3: só a demanda sem-demanda.</summary>
    internal static string ArquivoDaEtapa3() => $$"""
        {"demandas":[{"id":"sem-demanda","titulo":"Sem demanda","arquivada":false,"repositorios":[
          {"nome":"Faturamento","pasta":"Faturamento","sessoes":[
            {"agente":"claude","sessionId":"{{Id1}}","rotulo":"implementação","criadaEm":"2026-11-13T10:05:00-03:00"}]},
          {"nome":"Estoque","pasta":"Estoque","sessoes":[]}]}]}
        """;

    private static Demanda Demanda(DadosPainel dados, string id)
        => dados.Carregar().Demandas.Single(d => d.Id == id);

    private static string Texto(string caminho) => File.ReadAllText(caminho);

    // ---------- Arquivo de dados do painel ----------

    [Fact]
    public void Primeiro_uso_nao_cria_arquivo_e_nao_tem_demandas()
    {
        var (dados, pasta, caminho) = Novo();
        using var _ = pasta;
        var estado = dados.Carregar();
        Assert.True(estado.Disponivel);
        Assert.Empty(estado.Demandas);
        Assert.False(File.Exists(caminho));
        Assert.False(Directory.Exists(Path.GetDirectoryName(caminho)));
    }

    [Fact]
    public void Arquivo_gravado_pela_etapa_3_e_lido_sem_conversao()
    {
        var (dados, pasta, _) = Novo(ArquivoDaEtapa3());
        using var _p = pasta;
        var demanda = Assert.Single(dados.Carregar().Demandas);
        Assert.Equal("sem-demanda", demanda.Id);
        Assert.Equal("Sem demanda", demanda.Titulo);
        Assert.Null(demanda.OpenspecChange);
        Assert.Null(demanda.CriadaEm);
        Assert.False(demanda.Arquivada);
        Assert.Equal(["Faturamento", "Estoque"], demanda.Repositorios.Select(r => r.Pasta));

        var s = Assert.Single(demanda.Repositorios[0].Sessoes);
        Assert.Equal(Agente.Claude, s.Agente);
        Assert.Equal(Id1, s.SessionId);
        Assert.Equal("Faturamento", s.Pasta);
        Assert.Equal("sem-demanda", s.DemandaId);
        Assert.Equal("implementação", s.Rotulo);
        Assert.Equal(new DateTimeOffset(2026, 11, 13, 10, 5, 0, TimeSpan.FromHours(-3)), s.CriadaEm);
    }

    [Fact]
    public void Campos_da_demanda_sao_lidos()
    {
        var json = """{"demandas":[{"id":"CHG-142","titulo":"Ajuste NF-e","openspecChange":"ajuste-nfe","criadaEm":"2026-11-10T08:00:00-03:00","arquivada":true,"repositorios":[]}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        var d = Assert.Single(dados.Carregar().Demandas);
        Assert.Equal("ajuste-nfe", d.OpenspecChange);
        Assert.Equal(new DateTimeOffset(2026, 11, 10, 8, 0, 0, TimeSpan.FromHours(-3)), d.CriadaEm);
        Assert.True(d.Arquivada);
    }

    [Fact]
    public void Ausencia_de_campos_opcionais_e_aceita()
    {
        var (dados, pasta, _) = Novo("""{"demandas":[{"id":"CHG-1","titulo":"X"}]}""");
        using var _p = pasta;
        var d = Assert.Single(dados.Carregar().Demandas);
        Assert.False(d.Arquivada);
        Assert.Null(d.CriadaEm);
        Assert.Null(d.OpenspecChange);
        Assert.Empty(d.Repositorios);
    }

    // ---------- Validação ao carregar ----------

    [Fact]
    public void Pasta_invalida_editada_a_mao_e_desconsiderada()
    {
        var json = """{"demandas":[{"id":"D","titulo":"D","repositorios":[{"pasta":"..\\Windows","sessoes":[]},{"pasta":"..","sessoes":[]},{"pasta":"Ok","sessoes":[]}]}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Equal(["Ok"], dados.Carregar().Demandas.Single().Repositorios.Select(r => r.Pasta));
    }

    [Fact]
    public void Sessao_com_identificador_ou_agente_invalido_e_desconsiderada()
    {
        var json = $$"""
        {"demandas":[{"id":"D","titulo":"D","repositorios":[{"pasta":"App","sessoes":[
          {"agente":"claude","sessionId":"nao-e-guid"},
          {"agente":"outro","sessionId":"{{Id1}}"},
          {"agente":"claude","sessionId":"{{Id1}}\n"},
          {"agente":"codex","sessionId":"{{Id2}}"}]}]}]}
        """;
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        var s = Assert.Single(dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes);
        Assert.Equal(Id2, s.SessionId);
    }

    [Fact]
    public void Rotulo_invalido_no_arquivo_e_descartado()
    {
        var json = $$"""{"demandas":[{"id":"D","titulo":"D","repositorios":[{"pasta":"App","sessoes":[{"agente":"claude","sessionId":"{{Id1}}","rotulo":"a;b"}]}]}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Null(dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes.Single().Rotulo);
    }

    [Fact]
    public void Repositorios_duplicados_na_mesma_demanda_valem_uma_vez()
    {
        var json = """{"demandas":[{"id":"D","titulo":"D","repositorios":[{"pasta":"App"},{"pasta":"app"}]}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Single(dados.Carregar().Demandas.Single().Repositorios);
    }

    [Fact]
    public void Mesma_pasta_em_duas_demandas_e_carregada_nas_duas_com_sessoes_proprias()
    {
        var json = $$"""
        {"demandas":[
          {"id":"A","titulo":"A","repositorios":[{"pasta":"Faturamento","sessoes":[{"agente":"claude","sessionId":"{{Id1}}"}]}]},
          {"id":"B","titulo":"B","repositorios":[{"pasta":"Faturamento","sessoes":[{"agente":"claude","sessionId":"{{Id2}}"}]}]}]}
        """;
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        var demandas = dados.Carregar().Demandas;
        Assert.Equal(2, demandas.Count);
        Assert.Equal([Id1], demandas[0].Repositorios.Single().Sessoes.Select(s => s.SessionId));
        Assert.Equal([Id2], demandas[1].Repositorios.Single().Sessoes.Select(s => s.SessionId));
        Assert.Equal(["A", "B"], demandas.SelectMany(d => d.Repositorios.SelectMany(r => r.Sessoes)).Select(s => s.DemandaId));
    }

    [Theory]
    [InlineData("""{"demandas":[{"id":"a;b","titulo":"X","repositorios":[{"pasta":"App"}]}]}""")]
    [InlineData("""{"demandas":[{"id":"com espaço","titulo":"X"}]}""")]
    [InlineData("""{"demandas":[{"id":"..","titulo":"X"}]}""")]
    [InlineData("""{"demandas":[{"titulo":"X"}]}""")]
    [InlineData("""{"demandas":[{"id":"CHG-1"}]}""")]
    [InlineData("""{"demandas":[{"id":"CHG-1","titulo":""}]}""")]
    [InlineData("""{"demandas":[{"id":"CHG-1","titulo":"a\nb"}]}""")]
    [InlineData("""{"demandas":[{"id":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","titulo":"X"}]}""")]
    [InlineData("""{"demandas":[{"id":7,"titulo":"X"}]}""")]
    public void Demanda_invalida_e_desconsiderada(string json)
    {
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Empty(dados.Carregar().Demandas);
    }

    [Fact]
    public void Demanda_com_id_repetido_vale_a_primeira()
    {
        var json = """{"demandas":[{"id":"CHG-1","titulo":"Primeira"},{"id":"chg-1","titulo":"Segunda"}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Equal("Primeira", Assert.Single(dados.Carregar().Demandas).Titulo);
    }

    [Fact]
    public void Openspec_change_invalido_e_tratado_como_ausente()
    {
        var json = """{"demandas":[{"id":"A","titulo":"A","openspecChange":"..\\x"},{"id":"B","titulo":"B","openspecChange":"ok-1"}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        var d = dados.Carregar().Demandas;
        Assert.Null(d[0].OpenspecChange);
        Assert.Equal("ok-1", d[1].OpenspecChange);
    }

    [Fact]
    public void Sessao_repetida_em_duas_demandas_aparece_so_na_primeira()
    {
        var json = $$"""
        {"demandas":[
          {"id":"A","titulo":"A","repositorios":[{"pasta":"X","sessoes":[{"agente":"claude","sessionId":"{{Id1}}"}]}]},
          {"id":"B","titulo":"B","repositorios":[{"pasta":"X","sessoes":[{"agente":"claude","sessionId":"{{Id1}}"},{"agente":"claude","sessionId":"{{Id2}}"}]}]}]}
        """;
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        var d = dados.Carregar().Demandas;
        Assert.Equal([Id1], d[0].Repositorios.Single().Sessoes.Select(s => s.SessionId));
        Assert.Equal([Id2], d[1].Repositorios.Single().Sessoes.Select(s => s.SessionId));
    }

    [Fact]
    public void Sessao_repetida_no_mesmo_repositorio_sem_diferenciar_caixa_vale_uma_vez()
    {
        var json = $$"""{"demandas":[{"id":"A","titulo":"A","repositorios":[{"pasta":"X","sessoes":[{"agente":"claude","sessionId":"{{Id1}}"},{"agente":"codex","sessionId":"{{Id1.ToUpperInvariant()}}"}]}]}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Single(dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes);
    }

    [Fact]
    public void Sessao_de_demanda_invalida_nao_ocupa_o_identificador()
    {
        var json = $$"""
        {"demandas":[
          {"id":"a;b","titulo":"A","repositorios":[{"pasta":"X","sessoes":[{"agente":"claude","sessionId":"{{Id1}}"}]}]},
          {"id":"B","titulo":"B","repositorios":[{"pasta":"X","sessoes":[{"agente":"claude","sessionId":"{{Id1}}"}]}]}]}
        """;
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Equal([Id1], dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes.Select(s => s.SessionId));
    }

    // ---------- Criar e editar trabalho (só pelo título) ----------

    [Fact]
    public void Cria_trabalho_so_com_titulo_e_gera_o_identificador()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        var r = dados.CriarDemanda("Ajuste NF-e", Fabrica.Agora);
        Assert.True(r.Ok);
        Assert.Matches(@"\At[0-9a-f]{8}\z", r.Id!);

        var no = JsonNode.Parse(Texto(caminho))!["demandas"]!.AsArray().Single()!;
        Assert.Equal(r.Id, (string?)no["id"]);
        Assert.Equal("Ajuste NF-e", (string?)no["titulo"]);
        Assert.Equal(Fabrica.Agora, DateTimeOffset.Parse((string)no["criadaEm"]!));
        Assert.False((bool)no["arquivada"]!);
        Assert.Empty(no["repositorios"]!.AsArray());
        Assert.Null(no["openspecChange"]);
        Assert.Equal(Fabrica.Agora, Demanda(dados, r.Id!).CriadaEm);
    }

    [Fact]
    public void Identificador_gerado_nao_colide_com_os_existentes()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        var primeiro = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
        var segundo = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
        Assert.Equal("taaaaaaaa", dados.CriarDemanda("A", Fabrica.Agora, () => primeiro).Id);

        // O gerador repete o primeiro valor (também em maiúsculas no arquivo): o painel tenta outro.
        var guids = new Queue<Guid>([primeiro, primeiro, segundo]);
        var r = dados.CriarDemanda("B", Fabrica.Agora, guids.Dequeue);
        Assert.True(r.Ok);
        Assert.Equal("tbbbbbbbb", r.Id);
        Assert.Equal(2, dados.Carregar().Demandas.Count);
    }

    [Fact]
    public void Aceita_titulo_de_80_caracteres()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        Assert.True(dados.CriarDemanda(new string('t', 80), Fabrica.Agora).Ok);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a\nb")]
    [InlineData("tttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttt")] // 81
    public void Criacao_com_titulo_invalido_e_recusada_sem_alterar_o_arquivo(string titulo)
    {
        var original = """{"demandas":[]}""";
        var (dados, pasta, caminho) = Novo(original);
        using var _p = pasta;
        var r = dados.CriarDemanda(titulo, Fabrica.Agora);
        Assert.False(r.Ok);
        Assert.NotNull(r.Mensagem);
        Assert.Equal(original, Texto(caminho));
    }

    [Fact]
    public void Criar_trabalho_nao_cria_a_entrada_sessoes()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("X", Fabrica.Agora).Id!;
        dados.VincularRepositorio(id, "Faturamento");
        var demandas = JsonNode.Parse(Texto(caminho))!["demandas"]!.AsArray();
        Assert.Equal(id, (string?)Assert.Single(demandas)!["id"]);
    }

    [Fact]
    public void Editar_o_titulo_mantem_id_repositorios_e_sessoes()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("Antigo", Fabrica.Agora).Id!;
        dados.VincularRepositorio(id, "A");
        dados.RegistrarSessao(id, "A", Agente.Claude, Id1, null, Fabrica.Agora);

        Assert.True(dados.EditarDemanda(id, "Novo").Ok);
        var d = Demanda(dados, id);
        Assert.Equal("Novo", d.Titulo);
        Assert.Equal([Id1], d.Repositorios.Single().Sessoes.Select(s => s.SessionId));
    }

    [Fact]
    public void Editar_com_titulo_invalido_ou_trabalho_inexistente_e_recusado()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("A", Fabrica.Agora).Id!;
        var antes = Texto(caminho);
        Assert.False(dados.EditarDemanda(id, "").Ok);
        Assert.False(dados.EditarDemanda(id, "a\nb").Ok);
        Assert.False(dados.EditarDemanda("inexistente", "X").Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    [Fact]
    public void Editar_preserva_o_openspec_change_antigo_da_demanda()
    {
        var json = """{"demandas":[{"id":"CHG-1","titulo":"A","openspecChange":"antiga","repositorios":[]}]}""";
        var (dados, pasta, caminho) = Novo(json);
        using var _p = pasta;
        Assert.True(dados.EditarDemanda("CHG-1", "B").Ok);
        Assert.Equal("antiga", (string?)JsonNode.Parse(Texto(caminho))!["demandas"]![0]!["openspecChange"]);
    }

    [Fact]
    public void Sessoes_nao_pode_ser_editada_nem_arquivada()
    {
        var (dados, pasta, caminho) = Novo(ArquivoDaEtapa3());
        using var _p = pasta;
        var antes = Texto(caminho);
        Assert.False(dados.EditarDemanda("sem-demanda", "Outro").Ok);
        Assert.False(dados.ArquivarDemanda("sem-demanda").Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    // ---------- Arquivar e desarquivar ----------

    [Fact]
    public void Arquiva_e_desarquiva()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        Assert.True(dados.ArquivarDemanda("A").Ok);
        Assert.True(Demanda(dados, "A").Arquivada);
        Assert.True(dados.DesarquivarDemanda("A").Ok);
        Assert.False(Demanda(dados, "A").Arquivada);
    }

    [Fact]
    public void Arquivar_demanda_inexistente_falha()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        Assert.False(dados.ArquivarDemanda("X").Ok);
        Assert.False(dados.DesarquivarDemanda("X").Ok);
    }

    // ---------- Cadastro de repositórios ----------

    [Fact]
    public void Vincula_repositorio_a_demanda()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        Assert.True(dados.VincularRepositorio("A", "Faturamento").Ok);
        Assert.Equal(["Faturamento"], Demanda(dados, "A").Repositorios.Select(r => r.Pasta));
        var repo = JsonNode.Parse(Texto(caminho))!["demandas"]![0]!["repositorios"]![0]!;
        Assert.Equal("Faturamento", (string?)repo["nome"]);
        Assert.Equal("Faturamento", (string?)repo["pasta"]);
    }

    [Fact]
    public void Demanda_com_dois_repositorios()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "Faturamento");
        dados.VincularRepositorio("A", "Estoque");
        Assert.Equal(["Faturamento", "Estoque"], Demanda(dados, "A").Repositorios.Select(r => r.Pasta));
    }

    [Theory]
    [InlineData("com espaço")]
    [InlineData("a\\b")]
    [InlineData("a/b")]
    [InlineData("a;b")]
    [InlineData("..")]
    [InlineData("")]
    public void Vinculo_invalido_e_recusado_sem_alterar_o_arquivo(string nome)
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        var antes = Texto(caminho);
        var r = dados.VincularRepositorio("A", nome);
        Assert.False(r.Ok);
        Assert.NotNull(r.Mensagem);
        Assert.Equal(antes, Texto(caminho));
    }

    [Fact]
    public void Nome_repetido_na_mesma_demanda_sem_diferenciar_caixa_e_recusado()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "Faturamento");
        var antes = Texto(caminho);
        Assert.False(dados.VincularRepositorio("A", "faturamento").Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    [Fact]
    public void Mesma_pasta_em_outra_demanda_e_aceita()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.CriarDemanda("B", "B", null, Fabrica.Agora);
        Assert.True(dados.VincularRepositorio("A", "Faturamento").Ok);
        Assert.True(dados.VincularRepositorio("B", "Faturamento").Ok);
        Assert.All(dados.Carregar().Demandas, d => Assert.Equal(["Faturamento"], d.Repositorios.Select(r => r.Pasta)));
    }

    [Fact]
    public void Vincular_a_demanda_inexistente_falha()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        Assert.False(dados.VincularRepositorio("X", "A").Ok);
    }

    // ---------- Remoção de repositório ----------

    [Fact]
    public void Remove_repositorio_sem_sessoes_so_da_demanda()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "X");
        dados.VincularRepositorio("A", "Y");
        Assert.True(dados.RemoverRepositorio("A", "x").Ok);
        Assert.Equal(["Y"], Demanda(dados, "A").Repositorios.Select(r => r.Pasta));
    }

    [Fact]
    public void Remover_repositorio_inexistente_falha()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        Assert.False(dados.RemoverRepositorio("A", "B").Ok);
        Assert.False(dados.RemoverRepositorio("Z", "B").Ok);
    }

    [Fact]
    public void Remover_repositorio_leva_as_sessoes_dele()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "X");
        dados.RegistrarSessao("A", "X", Agente.Claude, Id1, null, Fabrica.Agora);
        Assert.True(dados.RemoverRepositorio("A", "X").Ok);
        Assert.Empty(Demanda(dados, "A").Repositorios);
    }

    [Fact]
    public void Remover_repositorio_nao_afeta_o_mesmo_repositorio_em_outra_demanda()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.CriarDemanda("B", "B", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "Faturamento");
        dados.VincularRepositorio("B", "Faturamento");
        dados.RegistrarSessao("B", "Faturamento", Agente.Claude, Id2, null, Fabrica.Agora);

        Assert.True(dados.RemoverRepositorio("A", "Faturamento").Ok);
        Assert.Empty(Demanda(dados, "A").Repositorios);
        var b = Demanda(dados, "B");
        Assert.Equal(["Faturamento"], b.Repositorios.Select(r => r.Pasta));
        Assert.Equal([Id2], b.Repositorios.Single().Sessoes.Select(s => s.SessionId));
    }

    // ---------- Registro de sessões ----------

    [Fact]
    public void Registra_sessao_do_claude_no_repositorio_da_demanda()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "Faturamento");
        Assert.True(dados.RegistrarSessao("A", "Faturamento", Agente.Claude, Id1, "implementação", Fabrica.Agora).Ok);
        var s = Demanda(dados, "A").Repositorios.Single().Sessoes.Single();
        Assert.Equal(Agente.Claude, s.Agente);
        Assert.Equal(Id1, s.SessionId);
        Assert.Equal("implementação", s.Rotulo);
        Assert.Equal(Fabrica.Agora, s.CriadaEm);
        Assert.Equal("A", s.DemandaId);
    }

    [Fact]
    public void Registra_na_demanda_certa_quando_a_pasta_esta_em_duas()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.CriarDemanda("B", "B", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "Faturamento");
        dados.VincularRepositorio("B", "Faturamento");
        dados.RegistrarSessao("B", "Faturamento", Agente.Claude, Id1, null, Fabrica.Agora);
        Assert.Empty(Demanda(dados, "A").Repositorios.Single().Sessoes);
        Assert.Single(Demanda(dados, "B").Repositorios.Single().Sessoes);
    }

    [Fact]
    public void Registro_em_repositorio_ou_demanda_inexistente_falha()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        Assert.False(dados.RegistrarSessao("A", "X", Agente.Claude, Id1, null, Fabrica.Agora).Ok);
        Assert.False(dados.RegistrarSessao("Z", "X", Agente.Claude, Id1, null, Fabrica.Agora).Ok);
    }

    [Fact]
    public void Registro_com_identificador_ou_rotulo_invalido_falha()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "X");
        Assert.False(dados.RegistrarSessao("A", "X", Agente.Claude, "x", null, Fabrica.Agora).Ok);
        Assert.False(dados.RegistrarSessao("A", "X", Agente.Claude, Id1, "a;b", Fabrica.Agora).Ok);
        Assert.Empty(Demanda(dados, "A").Repositorios.Single().Sessoes);
    }

    [Fact]
    public void Registro_de_identificador_ja_existente_no_arquivo_e_recusado()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.CriarDemanda("B", "B", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "X");
        dados.VincularRepositorio("B", "X");
        Assert.True(dados.RegistrarSessao("A", "X", Agente.Claude, Id1, null, Fabrica.Agora).Ok);
        Assert.False(dados.RegistrarSessao("B", "X", Agente.Claude, Id1.ToUpperInvariant(), null, Fabrica.Agora).Ok);
        Assert.Empty(Demanda(dados, "B").Repositorios.Single().Sessoes);
    }

    [Fact]
    public void Desfazer_remove_o_registro()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "X");
        dados.RegistrarSessao("A", "X", Agente.Claude, Id1, null, Fabrica.Agora);
        dados.RegistrarSessao("A", "X", Agente.Claude, Id2, null, Fabrica.Agora);
        Assert.True(dados.DesfazerRegistro(Id1).Ok);
        Assert.Equal([Id2], Demanda(dados, "A").Repositorios.Single().Sessoes.Select(s => s.SessionId));
    }

    // ---------- Vincular a um trabalho a partir de "Sessões" ----------

    private static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) ComSessaoEmSemDemanda()
    {
        var r = Novo(ArquivoDaEtapa3());
        r.Dados.CriarDemanda("CHG-142", "Ajuste NF-e", null, Fabrica.Agora);
        return r;
    }

    private static PedidoVinculo Pedido(string id, Agente agente = Agente.Claude)
        => PedidoVinculo.Criar(new Sessao(agente, id, @"C:\Fontes\Faturamento", StatusGravado.Ativa, Fabrica.Agora))!;

    [Fact]
    public void Vincular_sessao_registrada_em_sessoes_move_para_o_trabalho_e_vincula_o_repositorio()
    {
        var (dados, pasta, _) = ComSessaoEmSemDemanda();
        using var _p = pasta;
        Assert.True(dados.VincularSessao(Pedido(Id1), "CHG-142", "Faturamento", null, Fabrica.Agora).Ok);

        var semDemanda = Demanda(dados, "sem-demanda");
        Assert.Empty(semDemanda.Repositorios.Single(r => r.Pasta == "Faturamento").Sessoes);

        var repo = Assert.Single(Demanda(dados, "CHG-142").Repositorios);
        Assert.Equal("Faturamento", repo.Pasta);
        var s = Assert.Single(repo.Sessoes);
        Assert.Equal(Id1, s.SessionId);
        Assert.Equal("CHG-142", s.DemandaId);
        Assert.Equal("implementação", s.Rotulo); // sem rótulo escolhido, mantém o que tinha
    }

    [Fact]
    public void Vincular_sessao_movida_usa_o_repositorio_e_o_rotulo_escolhidos()
    {
        var (dados, pasta, _) = ComSessaoEmSemDemanda();
        using var _p = pasta;
        dados.VincularRepositorio("CHG-142", "Estoque");
        Assert.True(dados.VincularSessao(Pedido(Id1), "CHG-142", "Estoque", "novo", Fabrica.Agora).Ok);
        var s = Assert.Single(Demanda(dados, "CHG-142").Repositorios.Single().Sessoes);
        Assert.Equal("Estoque", s.Pasta);
        Assert.Equal("novo", s.Rotulo);
    }

    [Fact]
    public void Apos_vincular_o_identificador_consta_so_no_trabalho_de_destino()
    {
        var (dados, pasta, caminho) = ComSessaoEmSemDemanda();
        using var _p = pasta;
        dados.VincularSessao(Pedido(Id1), "CHG-142", "Faturamento", null, Fabrica.Agora);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(Texto(caminho), Id1));
    }

    [Fact]
    public void Vincular_preserva_campos_desconhecidos_da_sessao_movida()
    {
        var json = $$"""
        {"demandas":[
          {"id":"sem-demanda","titulo":"Sem demanda","repositorios":[{"pasta":"X","sessoes":[{"agente":"claude","sessionId":"{{Id1}}","nota":"guardar"}]}]},
          {"id":"B","titulo":"B","repositorios":[]}]}
        """;
        var (dados, pasta, caminho) = Novo(json);
        using var _p = pasta;
        Assert.True(dados.VincularSessao(Pedido(Id1), "B", "X", null, Fabrica.Agora).Ok);
        var no = JsonNode.Parse(Texto(caminho))!["demandas"]![1]!["repositorios"]![0]!["sessoes"]![0]!;
        Assert.Equal("guardar", (string?)no["nota"]);
    }

    [Fact]
    public void Sessao_registrada_em_um_trabalho_nao_e_movida_por_vinculo()
    {
        var (dados, pasta, caminho) = ComSessaoEmSemDemanda();
        using var _p = pasta;
        dados.VincularSessao(Pedido(Id1), "CHG-142", "Faturamento", null, Fabrica.Agora);
        dados.CriarDemanda("OUTRO", "Outro", null, Fabrica.Agora);
        var antes = Texto(caminho);
        Assert.False(dados.VincularSessao(Pedido(Id1), "OUTRO", "Faturamento", null, Fabrica.Agora).Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    [Fact]
    public void Vincular_a_trabalho_arquivado_ou_inexistente_falha_sem_alterar()
    {
        var (dados, pasta, caminho) = ComSessaoEmSemDemanda();
        using var _p = pasta;
        dados.ArquivarDemanda("CHG-142");
        var antes = Texto(caminho);
        Assert.False(dados.VincularSessao(Pedido(Id1), "CHG-142", "Faturamento", null, Fabrica.Agora).Ok);
        Assert.False(dados.VincularSessao(Pedido(Id1), "NAO-EXISTE", "Faturamento", null, Fabrica.Agora).Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    // ---------- Retomar sessão não registrada ----------

    [Fact]
    public void Retomada_registra_em_sessoes_no_repositorio_escolhido_com_vinculadaEm()
    {
        var (dados, pasta, caminho) = Novo(ArquivoDaEtapa3());
        using var _p = pasta;
        Assert.True(dados.RegistrarRetomada(Pedido(Id2, Agente.Codex), "Estoque", Fabrica.Agora).Ok);
        var s = Assert.Single(Demanda(dados, "sem-demanda").Repositorios.Single(r => r.Pasta == "Estoque").Sessoes);
        Assert.Equal(Id2, s.SessionId);
        Assert.Equal(Agente.Codex, s.Agente);
        var no = JsonNode.Parse(Texto(caminho))!["demandas"]![0]!["repositorios"]![1]!["sessoes"]![0]!;
        Assert.Equal(Fabrica.Agora, DateTimeOffset.Parse((string)no["vinculadaEm"]!));
    }

    [Fact]
    public void Retomada_cria_a_entrada_sessoes_quando_o_arquivo_ainda_nao_a_tem()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        Assert.True(dados.RegistrarRetomada(Pedido(Id2), "Estoque", Fabrica.Agora).Ok);
        var d = Assert.Single(dados.Carregar().Demandas);
        Assert.Equal("sem-demanda", d.Id);
        Assert.Equal(Id2, d.Repositorios.Single().Sessoes.Single().SessionId);
    }

    [Fact]
    public void Retomada_de_sessao_ja_registrada_falha()
    {
        var (dados, pasta, caminho) = Novo(ArquivoDaEtapa3());
        using var _p = pasta;
        var antes = Texto(caminho);
        Assert.False(dados.RegistrarRetomada(Pedido(Id1), "Faturamento", Fabrica.Agora).Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    // ---------- Remover sessão ----------

    [Fact]
    public void Remove_o_registro_de_uma_sessao()
    {
        var (dados, pasta, _) = Novo(ArquivoDaEtapa3());
        using var _p = pasta;
        Assert.True(dados.DesfazerRegistro(Id1).Ok);
        Assert.Empty(Demanda(dados, "sem-demanda").Repositorios.Single(r => r.Pasta == "Faturamento").Sessoes);
    }

    [Fact]
    public void Remover_sessao_desconhecida_falha()
    {
        var (dados, pasta, _) = Novo(ArquivoDaEtapa3());
        using var _p = pasta;
        Assert.False(dados.DesfazerRegistro(Id3).Ok);
    }

    // ---------- Arquivo ilegível e gravação segura ----------

    [Theory]
    [InlineData("isto não é JSON")]
    [InlineData("")]
    [InlineData("[1,2]")]
    [InlineData("""{"demandas":"x"}""")]
    [InlineData("""{"demandas":[1]}""")]
    [InlineData("""{"demandas":[{"repositorios":{}}]}""")]
    [InlineData("""{"demandas":[{"repositorios":[{"pasta":"A","sessoes":5}]}]}""")]
    [InlineData("""{"demandas":[{"repositorios":[]}""")]
    public void Arquivo_ilegivel_fica_indisponivel_e_nao_e_alterado(string conteudo)
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        var bytes = System.Text.Encoding.UTF8.GetBytes(conteudo);
        File.WriteAllBytes(caminho, bytes);

        var estado = dados.Carregar();
        Assert.False(estado.Disponivel);
        Assert.NotNull(estado.Erro);
        Assert.Empty(estado.Demandas);

        Assert.False(dados.CriarDemanda("A", "A", null, Fabrica.Agora).Ok);
        Assert.False(dados.EditarDemanda("A", "A").Ok);
        Assert.False(dados.ArquivarDemanda("A").Ok);
        Assert.False(dados.DesarquivarDemanda("A").Ok);
        Assert.False(dados.VincularRepositorio("A", "X").Ok);
        Assert.False(dados.RemoverRepositorio("A", "X").Ok);
        Assert.False(dados.RegistrarSessao("A", "X", Agente.Claude, Id1, null, Fabrica.Agora).Ok);
        Assert.False(dados.ExcluirSessao(Agente.Claude, Id1, Fabrica.Agora).Ok);
        Assert.False(dados.DesfazerRegistro(Id1).Ok);
        Assert.False(dados.DesfazerRegistro(Id1).Ok);

        Assert.Equal(bytes, File.ReadAllBytes(caminho));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(caminho)!));
    }

    [Fact]
    public void Operacoes_novas_preservam_campos_desconhecidos()
    {
        var json = $$"""
        {"versao":2,"demandas":[{"id":"CHG-1","titulo":"X","extra":{"a":[1,2]},"repositorios":[
          {"nome":"A","pasta":"A","campoNovo":true,"sessoes":[{"agente":"claude","sessionId":"{{Id1}}","outro":"v"}]}]}]}
        """;
        var (dados, pasta, caminho) = Novo(json);
        using var _p = pasta;

        Assert.True(dados.CriarDemanda("CHG-2", "Y", null, Fabrica.Agora).Ok);
        Assert.True(dados.EditarDemanda("CHG-1", "X2").Ok);
        Assert.True(dados.VincularRepositorio("CHG-2", "B").Ok);
        Assert.True(dados.ArquivarDemanda("CHG-2").Ok);
        Assert.True(dados.DesarquivarDemanda("CHG-2").Ok);
        Assert.True(dados.RegistrarSessao("CHG-1", "A", Agente.Codex, Id2, null, Fabrica.Agora).Ok);
        Assert.True(dados.DesfazerRegistro(Id2).Ok);
        Assert.True(dados.RemoverRepositorio("CHG-2", "B").Ok);

        var raiz = JsonNode.Parse(Texto(caminho))!;
        Assert.Equal(2, (int?)raiz["versao"]);
        var demanda = raiz["demandas"]![0]!;
        Assert.Equal("X2", (string?)demanda["titulo"]);
        Assert.Equal(2, (int?)demanda["extra"]!["a"]![1]);
        Assert.True((bool?)demanda["repositorios"]![0]!["campoNovo"]);
        Assert.Equal("v", (string?)demanda["repositorios"]![0]!["sessoes"]![0]!["outro"]);
    }

    [Fact]
    public void Falha_na_gravacao_mantem_o_conteudo_anterior_e_nao_deixa_temporario()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        Assert.True(dados.CriarDemanda("A", "A", null, Fabrica.Agora).Ok);
        var antes = File.ReadAllBytes(caminho);

        // Um leitor sem compartilhamento de escrita faz a troca falhar.
        using (new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var r = dados.CriarDemanda("B", "B", null, Fabrica.Agora);
            Assert.False(r.Ok);
        }

        Assert.Equal(antes, File.ReadAllBytes(caminho));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(caminho)!));
        Assert.Equal(["A"], dados.Carregar().Demandas.Select(d => d.Id));
    }

    [Fact]
    public void Gravacao_nao_deixa_temporario_e_usa_utf8_sem_bom()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "Implementação", null, Fabrica.Agora);
        dados.VincularRepositorio("A", "X");
        dados.RegistrarSessao("A", "X", Agente.Claude, Id1, "implementação", Fabrica.Agora);
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(caminho)!));
        var bytes = File.ReadAllBytes(caminho);
        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Contains("implementação", System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Arquivo_com_bom_e_lido()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        File.WriteAllText(caminho, """{"demandas":[{"id":"A","titulo":"A"}]}""", new System.Text.UTF8Encoding(true));
        Assert.Single(dados.Carregar().Demandas);
    }
}
