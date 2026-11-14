using System.Text.Json.Nodes;
using DataPilot.Core;

namespace DataPilot.Core.Tests;

public class DadosPainelTests
{
    private const string Id1 = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    private const string Id2 = "11111111-2222-3333-4444-555555555555";

    private static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) Novo(string? conteudo = null)
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

    // ---------- Arquivo de dados do painel ----------

    [Fact]
    public void Primeiro_uso_nao_cria_arquivo_e_nao_tem_repositorios()
    {
        var (dados, pasta, caminho) = Novo();
        using var _ = pasta;
        var estado = dados.Carregar();
        Assert.True(estado.Disponivel);
        Assert.Empty(estado.Repositorios);
        Assert.False(File.Exists(caminho));
        Assert.False(Directory.Exists(Path.GetDirectoryName(caminho)));
    }

    [Fact]
    public void Dados_existentes_ficam_disponiveis()
    {
        var json = $$"""
        {"demandas":[{"id":"sem-demanda","titulo":"Sem demanda","repositorios":[
          {"nome":"Faturamento","pasta":"Faturamento","sessoes":[
            {"agente":"claude","sessionId":"{{Id1}}","rotulo":"implementação","criadaEm":"2026-11-13T10:05:00-03:00"}]}]}]}
        """;
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        var repo = Assert.Single(dados.Carregar().Repositorios);
        Assert.Equal("Faturamento", repo.Pasta);
        var s = Assert.Single(repo.Sessoes);
        Assert.Equal(Agente.Claude, s.Agente);
        Assert.Equal(Id1, s.SessionId);
        Assert.Equal("Faturamento", s.Pasta);
        Assert.Equal("implementação", s.Rotulo);
        Assert.Equal(new DateTimeOffset(2026, 11, 13, 10, 5, 0, TimeSpan.FromHours(-3)), s.CriadaEm);
    }

    // ---------- Validação ao carregar ----------

    [Fact]
    public void Pasta_invalida_editada_a_mao_e_desconsiderada()
    {
        var json = """{"demandas":[{"repositorios":[{"pasta":"..\\Windows","sessoes":[]},{"pasta":"..","sessoes":[]},{"pasta":"Ok","sessoes":[]}]}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Equal(["Ok"], dados.Carregar().Repositorios.Select(r => r.Pasta));
    }

    [Fact]
    public void Sessao_com_identificador_ou_agente_invalido_e_desconsiderada()
    {
        var json = $$"""
        {"demandas":[{"repositorios":[{"pasta":"App","sessoes":[
          {"agente":"claude","sessionId":"nao-e-guid"},
          {"agente":"outro","sessionId":"{{Id1}}"},
          {"agente":"claude","sessionId":"{{Id1}}\n"},
          {"agente":"codex","sessionId":"{{Id2}}"}]}]}]}
        """;
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        var s = Assert.Single(dados.Carregar().Repositorios.Single().Sessoes);
        Assert.Equal(Id2, s.SessionId);
    }

    [Fact]
    public void Rotulo_invalido_no_arquivo_e_descartado()
    {
        var json = $$"""{"demandas":[{"repositorios":[{"pasta":"App","sessoes":[{"agente":"claude","sessionId":"{{Id1}}","rotulo":"a;b"}]}]}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Null(dados.Carregar().Repositorios.Single().Sessoes.Single().Rotulo);
    }

    [Fact]
    public void Repositorios_duplicados_no_arquivo_valem_uma_vez()
    {
        var json = """{"demandas":[{"repositorios":[{"pasta":"App"},{"pasta":"app"}]}]}""";
        var (dados, pasta, _) = Novo(json);
        using var _p = pasta;
        Assert.Single(dados.Carregar().Repositorios);
    }

    // ---------- Demanda implícita e cadastro ----------

    [Fact]
    public void Primeiro_cadastro_cria_demanda_implicita()
    {
        var (dados, pasta, caminho) = Novo();
        using var _ = pasta;
        Assert.True(dados.Cadastrar("Faturamento").Ok);

        var raiz = JsonNode.Parse(File.ReadAllText(caminho))!;
        var demanda = raiz["demandas"]!.AsArray().Single()!;
        Assert.Equal("sem-demanda", (string?)demanda["id"]);
        Assert.Equal("Sem demanda", (string?)demanda["titulo"]);
        var repo = demanda["repositorios"]!.AsArray().Single()!;
        Assert.Equal("Faturamento", (string?)repo["pasta"]);
        Assert.Equal("Faturamento", (string?)repo["nome"]);
    }

    [Fact]
    public void Segundo_cadastro_reusa_a_demanda_implicita()
    {
        var (dados, pasta, caminho) = Novo();
        using var _ = pasta;
        dados.Cadastrar("A");
        dados.Cadastrar("B");
        var raiz = JsonNode.Parse(File.ReadAllText(caminho))!;
        Assert.Single(raiz["demandas"]!.AsArray());
        Assert.Equal(2, raiz["demandas"]![0]!["repositorios"]!.AsArray().Count);
    }

    [Theory]
    [InlineData("com espaço")]
    [InlineData("a\\b")]
    [InlineData("a/b")]
    [InlineData("a;b")]
    [InlineData("..")]
    [InlineData("")]
    public void Cadastro_invalido_e_recusado_sem_alterar_o_arquivo(string nome)
    {
        var original = """{"demandas":[]}""";
        var (dados, pasta, caminho) = Novo(original);
        using var _ = pasta;
        var r = dados.Cadastrar(nome);
        Assert.False(r.Ok);
        Assert.NotNull(r.Mensagem);
        Assert.Equal(original, File.ReadAllText(caminho));
    }

    [Fact]
    public void Cadastro_invalido_nao_cria_arquivo()
    {
        var (dados, pasta, caminho) = Novo();
        using var _ = pasta;
        Assert.False(dados.Cadastrar("..").Ok);
        Assert.False(File.Exists(caminho));
    }

    [Fact]
    public void Nome_repetido_sem_diferenciar_caixa_e_recusado()
    {
        var (dados, pasta, caminho) = Novo();
        using var _ = pasta;
        Assert.True(dados.Cadastrar("Faturamento").Ok);
        var antes = File.ReadAllText(caminho);
        var r = dados.Cadastrar("faturamento");
        Assert.False(r.Ok);
        Assert.Equal(antes, File.ReadAllText(caminho));
        Assert.Single(dados.Carregar().Repositorios);
    }

    // ---------- Remoção ----------

    [Fact]
    public void Remove_repositorio_sem_sessoes()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.Cadastrar("A");
        dados.Cadastrar("B");
        Assert.True(dados.Remover("a").Ok);
        Assert.Equal(["B"], dados.Carregar().Repositorios.Select(r => r.Pasta));
    }

    [Fact]
    public void Remover_repositorio_inexistente_falha()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.Cadastrar("A");
        Assert.False(dados.Remover("B").Ok);
    }

    [Fact]
    public void Remover_repositorio_leva_as_sessoes()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.Cadastrar("A");
        dados.RegistrarSessao("A", Agente.Claude, Id1, null, Fabrica.Agora);
        Assert.Single(dados.Carregar().Repositorios.Single().Sessoes);
        Assert.True(dados.Remover("A").Ok);
        Assert.Empty(dados.Carregar().Repositorios);
    }

    // ---------- Registro de sessões ----------

    [Fact]
    public void Registra_sessao_do_claude_no_repositorio()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.Cadastrar("Faturamento");
        Assert.True(dados.RegistrarSessao("Faturamento", Agente.Claude, Id1, "implementação", Fabrica.Agora).Ok);
        var s = dados.Carregar().Repositorios.Single().Sessoes.Single();
        Assert.Equal(Agente.Claude, s.Agente);
        Assert.Equal(Id1, s.SessionId);
        Assert.Equal("implementação", s.Rotulo);
        Assert.Equal(Fabrica.Agora, s.CriadaEm);
    }

    [Fact]
    public void Registro_em_repositorio_nao_cadastrado_falha()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        Assert.False(dados.RegistrarSessao("X", Agente.Claude, Id1, null, Fabrica.Agora).Ok);
    }

    [Fact]
    public void Registro_com_identificador_ou_rotulo_invalido_falha()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.Cadastrar("A");
        Assert.False(dados.RegistrarSessao("A", Agente.Claude, "x", null, Fabrica.Agora).Ok);
        Assert.False(dados.RegistrarSessao("A", Agente.Claude, Id1, "a;b", Fabrica.Agora).Ok);
        Assert.Empty(dados.Carregar().Repositorios.Single().Sessoes);
    }

    [Fact]
    public void Desfazer_remove_o_registro()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        dados.Cadastrar("A");
        dados.RegistrarSessao("A", Agente.Claude, Id1, null, Fabrica.Agora);
        dados.RegistrarSessao("A", Agente.Claude, Id2, null, Fabrica.Agora);
        Assert.True(dados.DesfazerRegistro(Id1).Ok);
        Assert.Equal([Id2], dados.Carregar().Repositorios.Single().Sessoes.Select(s => s.SessionId));
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
        Assert.Empty(estado.Repositorios);

        Assert.False(dados.Cadastrar("A").Ok);
        Assert.False(dados.Remover("A").Ok);
        Assert.False(dados.RegistrarSessao("A", Agente.Claude, Id1, null, Fabrica.Agora).Ok);
        Assert.False(dados.DesfazerRegistro(Id1).Ok);

        Assert.Equal(bytes, File.ReadAllBytes(caminho));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(caminho)!));
    }

    [Fact]
    public void Campos_desconhecidos_sao_preservados()
    {
        var json = $$"""
        {"versao":2,"demandas":[{"id":"CHG-1","titulo":"X","extra":{"a":[1,2]},"repositorios":[
          {"nome":"A","pasta":"A","campoNovo":true,"sessoes":[{"agente":"claude","sessionId":"{{Id1}}","outro":"v"}]}]}]}
        """;
        var (dados, pasta, caminho) = Novo(json);
        using var _p = pasta;
        Assert.True(dados.Cadastrar("B").Ok);

        var raiz = JsonNode.Parse(File.ReadAllText(caminho))!;
        Assert.Equal(2, (int?)raiz["versao"]);
        var demanda = raiz["demandas"]![0]!;
        Assert.Equal(2, (int?)demanda["extra"]!["a"]![1]);
        Assert.True((bool?)demanda["repositorios"]![0]!["campoNovo"]);
        Assert.Equal("v", (string?)demanda["repositorios"]![0]!["sessoes"]![0]!["outro"]);
        // O repositório novo vai para a demanda implícita, que é criada ao lado da existente.
        Assert.Equal(2, raiz["demandas"]!.AsArray().Count);
        Assert.Equal("sem-demanda", (string?)raiz["demandas"]![1]!["id"]);
    }

    [Fact]
    public void Falha_na_gravacao_mantem_o_conteudo_anterior_e_nao_deixa_temporario()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        Assert.True(dados.Cadastrar("A").Ok);
        var antes = File.ReadAllBytes(caminho);

        // Um leitor sem compartilhamento de escrita faz a troca falhar.
        using (new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var r = dados.Cadastrar("B");
            Assert.False(r.Ok);
        }

        Assert.Equal(antes, File.ReadAllBytes(caminho));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(caminho)!));
        Assert.Equal(["A"], dados.Carregar().Repositorios.Select(r => r.Pasta));
    }

    [Fact]
    public void Gravacao_nao_deixa_temporario_e_usa_utf8_sem_bom()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        dados.Cadastrar("A");
        dados.RegistrarSessao("A", Agente.Claude, Id1, "implementação", Fabrica.Agora);
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
        File.WriteAllText(caminho, """{"demandas":[{"repositorios":[{"pasta":"A"}]}]}""", new System.Text.UTF8Encoding(true));
        Assert.Single(dados.Carregar().Repositorios);
    }
}
