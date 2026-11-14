using System.Text.Json.Nodes;
using DataPilot.Core;

namespace DataPilot.Core.Tests;

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

    // ---------- Criar demanda ----------

    [Fact]
    public void Cria_demanda_valida_com_data_e_campos_iniciais()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        Assert.True(dados.CriarDemanda("CHG-142", "Ajuste NF-e", null, Fabrica.Agora).Ok);

        var no = JsonNode.Parse(Texto(caminho))!["demandas"]!.AsArray().Single()!;
        Assert.Equal("CHG-142", (string?)no["id"]);
        Assert.Equal("Ajuste NF-e", (string?)no["titulo"]);
        Assert.Equal(Fabrica.Agora, DateTimeOffset.Parse((string)no["criadaEm"]!));
        Assert.False((bool)no["arquivada"]!);
        Assert.Empty(no["repositorios"]!.AsArray());
        Assert.Null(no["openspecChange"]);

        var d = Demanda(dados, "CHG-142");
        Assert.Equal(Fabrica.Agora, d.CriadaEm);
    }

    [Fact]
    public void Cria_demanda_com_openspec_change()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        Assert.True(dados.CriarDemanda("CHG-1", "X", "minha-change", Fabrica.Agora).Ok);
        Assert.Equal("minha-change", Demanda(dados, "CHG-1").OpenspecChange);
    }

    [Fact]
    public void Aceita_limites_de_30_e_80_caracteres()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        Assert.True(dados.CriarDemanda(new string('a', 30), new string('t', 80), null, Fabrica.Agora).Ok);
    }

    [Theory]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "T")]       // 31 caracteres no id
    [InlineData("CHG-1", "ttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttt")] // 81 no título
    [InlineData("..", "T")]
    [InlineData("a;b", "T")]
    [InlineData("com espaço", "T")]
    [InlineData("", "T")]
    [InlineData("CHG-1", "")]
    [InlineData("CHG-1", "a\nb")]
    [InlineData("sem-demanda", "Sem demanda")]
    public void Criacao_invalida_e_recusada_sem_alterar_o_arquivo(string id, string titulo)
    {
        var original = """{"demandas":[]}""";
        var (dados, pasta, caminho) = Novo(original);
        using var _p = pasta;
        var r = dados.CriarDemanda(id, titulo, null, Fabrica.Agora);
        Assert.False(r.Ok);
        Assert.NotNull(r.Mensagem);
        Assert.Equal(original, Texto(caminho));
    }

    [Fact]
    public void Openspec_change_invalido_na_criacao_e_recusado()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        Assert.False(dados.CriarDemanda("CHG-1", "X", "a b", Fabrica.Agora).Ok);
        Assert.False(File.Exists(caminho));
    }

    [Fact]
    public void Identificador_repetido_sem_diferenciar_caixa_e_recusado()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        Assert.True(dados.CriarDemanda("CHG-142", "A", null, Fabrica.Agora).Ok);
        var antes = Texto(caminho);
        var r = dados.CriarDemanda("chg-142", "B", null, Fabrica.Agora);
        Assert.False(r.Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    [Fact]
    public void Primeira_demanda_nao_cria_a_demanda_implicita()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("CHG-1", "X", null, Fabrica.Agora);
        dados.VincularRepositorio("CHG-1", "Faturamento");
        var demandas = JsonNode.Parse(Texto(caminho))!["demandas"]!.AsArray();
        Assert.Equal("CHG-1", (string?)Assert.Single(demandas)!["id"]);
    }

    // ---------- Editar demanda ----------

    [Fact]
    public void Editar_o_titulo_mantem_repositorios_e_sessoes()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("CHG-1", "Antigo", null, Fabrica.Agora);
        dados.VincularRepositorio("CHG-1", "A");
        dados.RegistrarSessao("CHG-1", "A", Agente.Claude, Id1, null, Fabrica.Agora);

        Assert.True(dados.EditarDemanda("CHG-1", "CHG-1", "Novo", null).Ok);
        var d = Demanda(dados, "CHG-1");
        Assert.Equal("Novo", d.Titulo);
        Assert.Equal([Id1], d.Repositorios.Single().Sessoes.Select(s => s.SessionId));
    }

    [Fact]
    public void Editar_o_identificador_move_a_demanda_inteira_para_o_novo_id()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("CHG-1", "X", null, Fabrica.Agora);
        dados.VincularRepositorio("CHG-1", "A");
        dados.RegistrarSessao("CHG-1", "A", Agente.Claude, Id1, null, Fabrica.Agora);

        Assert.True(dados.EditarDemanda("CHG-1", "CHG-9", "X", "chg-nove").Ok);
        var d = Assert.Single(dados.Carregar().Demandas);
        Assert.Equal("CHG-9", d.Id);
        Assert.Equal("chg-nove", d.OpenspecChange);
        Assert.Equal("CHG-9", d.Repositorios.Single().Sessoes.Single().DemandaId);
    }

    [Fact]
    public void Editar_pode_remover_o_openspec_change()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("CHG-1", "X", "abc", Fabrica.Agora);
        Assert.True(dados.EditarDemanda("CHG-1", "CHG-1", "X", null).Ok);
        Assert.Null(Demanda(dados, "CHG-1").OpenspecChange);
        Assert.Null(JsonNode.Parse(Texto(caminho))!["demandas"]![0]!["openspecChange"]);
    }

    [Fact]
    public void Editar_o_id_para_o_de_outra_demanda_e_recusado()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        dados.CriarDemanda("B", "B", null, Fabrica.Agora);
        var antes = Texto(caminho);
        Assert.False(dados.EditarDemanda("A", "b", "A", null).Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    [Fact]
    public void Editar_mudando_so_a_caixa_do_proprio_id_e_aceito()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("chg-1", "A", null, Fabrica.Agora);
        Assert.True(dados.EditarDemanda("chg-1", "CHG-1", "A", null).Ok);
        Assert.Equal("CHG-1", Assert.Single(dados.Carregar().Demandas).Id);
    }

    [Fact]
    public void Editar_com_dados_invalidos_e_recusado()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        var antes = Texto(caminho);
        Assert.False(dados.EditarDemanda("A", "a;b", "A", null).Ok);
        Assert.False(dados.EditarDemanda("A", "A", "", null).Ok);
        Assert.False(dados.EditarDemanda("A", "A", "A", "a b").Ok);
        Assert.False(dados.EditarDemanda("inexistente", "X", "X", null).Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    [Fact]
    public void Sem_demanda_nao_pode_ser_editada_nem_arquivada()
    {
        var (dados, pasta, caminho) = Novo(ArquivoDaEtapa3());
        using var _p = pasta;
        var antes = Texto(caminho);
        Assert.False(dados.EditarDemanda("sem-demanda", "outro", "Outro", null).Ok);
        Assert.False(dados.ArquivarDemanda("sem-demanda").Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    [Fact]
    public void Nenhuma_demanda_pode_receber_o_id_reservado_por_edicao()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.CriarDemanda("A", "A", null, Fabrica.Agora);
        Assert.False(dados.EditarDemanda("A", "sem-demanda", "A", null).Ok);
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

    // ---------- Mover sessão ----------

    private static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) ComSessaoEmSemDemanda()
    {
        var r = Novo(ArquivoDaEtapa3());
        r.Dados.CriarDemanda("CHG-142", "Ajuste NF-e", null, Fabrica.Agora);
        return r;
    }

    [Fact]
    public void Move_de_sem_demanda_para_uma_demanda_sem_o_repositorio_e_vincula_o_repositorio()
    {
        var (dados, pasta, _) = ComSessaoEmSemDemanda();
        using var _p = pasta;
        Assert.True(dados.MoverSessao(Id1, "CHG-142").Ok);

        var semDemanda = Demanda(dados, "sem-demanda");
        Assert.Empty(semDemanda.Repositorios.Single(r => r.Pasta == "Faturamento").Sessoes);

        var destino = Demanda(dados, "CHG-142");
        var repo = Assert.Single(destino.Repositorios);
        Assert.Equal("Faturamento", repo.Pasta);
        var s = Assert.Single(repo.Sessoes);
        Assert.Equal(Id1, s.SessionId);
        Assert.Equal("CHG-142", s.DemandaId);
        Assert.Equal("implementação", s.Rotulo);
    }

    [Fact]
    public void Move_para_o_repositorio_de_mesma_pasta_que_a_demanda_ja_tem()
    {
        var (dados, pasta, _) = ComSessaoEmSemDemanda();
        using var _p = pasta;
        dados.VincularRepositorio("CHG-142", "faturamento");
        dados.RegistrarSessao("CHG-142", "faturamento", Agente.Codex, Id2, null, Fabrica.Agora);

        Assert.True(dados.MoverSessao(Id1, "CHG-142").Ok);
        var repo = Assert.Single(Demanda(dados, "CHG-142").Repositorios);
        Assert.Equal("faturamento", repo.Pasta);
        Assert.Equal([Id2, Id1], repo.Sessoes.Select(s => s.SessionId));
    }

    [Fact]
    public void Apos_mover_o_identificador_consta_so_na_demanda_de_destino()
    {
        var (dados, pasta, caminho) = ComSessaoEmSemDemanda();
        using var _p = pasta;
        dados.MoverSessao(Id1, "CHG-142");
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(Texto(caminho), Id1));
    }

    [Fact]
    public void Mover_preserva_campos_desconhecidos_da_sessao()
    {
        var json = $$"""
        {"demandas":[
          {"id":"A","titulo":"A","repositorios":[{"pasta":"X","sessoes":[{"agente":"claude","sessionId":"{{Id1}}","nota":"guardar"}]}]},
          {"id":"B","titulo":"B","repositorios":[]}]}
        """;
        var (dados, pasta, caminho) = Novo(json);
        using var _p = pasta;
        Assert.True(dados.MoverSessao(Id1, "B").Ok);
        var no = JsonNode.Parse(Texto(caminho))!["demandas"]![1]!["repositorios"]![0]!["sessoes"]![0]!;
        Assert.Equal("guardar", (string?)no["nota"]);
    }

    [Fact]
    public void Mover_com_destino_igual_inexistente_ou_sessao_desconhecida_falha_sem_alterar()
    {
        var (dados, pasta, caminho) = ComSessaoEmSemDemanda();
        using var _p = pasta;
        var antes = Texto(caminho);
        Assert.False(dados.MoverSessao(Id1, "sem-demanda").Ok);
        Assert.False(dados.MoverSessao(Id1, "NAO-EXISTE").Ok);
        Assert.False(dados.MoverSessao(Id3, "CHG-142").Ok);
        Assert.Equal(antes, Texto(caminho));
    }

    // ---------- Remover sessão ----------

    [Fact]
    public void Remove_o_registro_de_uma_sessao()
    {
        var (dados, pasta, _) = Novo(ArquivoDaEtapa3());
        using var _p = pasta;
        Assert.True(dados.RemoverSessao(Id1).Ok);
        Assert.Empty(Demanda(dados, "sem-demanda").Repositorios.Single(r => r.Pasta == "Faturamento").Sessoes);
    }

    [Fact]
    public void Remover_sessao_desconhecida_falha()
    {
        var (dados, pasta, _) = Novo(ArquivoDaEtapa3());
        using var _p = pasta;
        Assert.False(dados.RemoverSessao(Id3).Ok);
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
        Assert.False(dados.EditarDemanda("A", "A", "A", null).Ok);
        Assert.False(dados.ArquivarDemanda("A").Ok);
        Assert.False(dados.DesarquivarDemanda("A").Ok);
        Assert.False(dados.VincularRepositorio("A", "X").Ok);
        Assert.False(dados.RemoverRepositorio("A", "X").Ok);
        Assert.False(dados.RegistrarSessao("A", "X", Agente.Claude, Id1, null, Fabrica.Agora).Ok);
        Assert.False(dados.MoverSessao(Id1, "B").Ok);
        Assert.False(dados.RemoverSessao(Id1).Ok);
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
        Assert.True(dados.EditarDemanda("CHG-1", "CHG-1", "X2", "oc").Ok);
        Assert.True(dados.VincularRepositorio("CHG-2", "B").Ok);
        Assert.True(dados.ArquivarDemanda("CHG-2").Ok);
        Assert.True(dados.DesarquivarDemanda("CHG-2").Ok);
        Assert.True(dados.RegistrarSessao("CHG-1", "A", Agente.Codex, Id2, null, Fabrica.Agora).Ok);
        Assert.True(dados.RemoverSessao(Id2).Ok);
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
