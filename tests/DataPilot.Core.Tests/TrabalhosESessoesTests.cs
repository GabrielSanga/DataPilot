using System.Diagnostics;
using System.Text.Json.Nodes;
using DataPilot.Core;

namespace DataPilot.Core.Tests;

public class ChangeNoRepositorioTests
{
    private static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) Novo(string? json = null)
        => DadosPainelTests.Novo(json);

    [Fact]
    public void Change_escolhida_fica_no_repositorio_e_nao_na_demanda()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("X", Fabrica.Agora).Id!;
        Assert.True(dados.VincularRepositorio(id, "Faturamento", "ajuste-boleto").Ok);

        var demanda = JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!;
        Assert.Equal("ajuste-boleto", (string?)demanda["repositorios"]![0]!["openspecChange"]);
        Assert.Null(demanda["openspecChange"]);
        Assert.Equal("ajuste-boleto", dados.Carregar().Demandas.Single().Repositorios.Single().OpenspecChange);
    }

    [Fact]
    public void Trocar_a_change_grava_so_o_repositorio()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("X", Fabrica.Agora).Id!;
        dados.VincularRepositorio(id, "A", "uma");
        dados.VincularRepositorio(id, "B", "outra");
        Assert.True(dados.DefinirChange(id, "A", "nova").Ok);
        var repos = dados.Carregar().Demandas.Single().Repositorios;
        Assert.Equal("nova", repos.Single(r => r.Pasta == "A").OpenspecChange);
        Assert.Equal("outra", repos.Single(r => r.Pasta == "B").OpenspecChange);
    }

    [Fact]
    public void Nenhuma_remove_a_change_do_repositorio()
    {
        var (dados, pasta, _) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("X", Fabrica.Agora).Id!;
        dados.VincularRepositorio(id, "A", "uma");
        Assert.True(dados.DefinirChange(id, "A", null).Ok);
        Assert.Null(dados.Carregar().Demandas.Single().Repositorios.Single().OpenspecChange);
    }

    [Fact]
    public void Change_invalida_e_recusada_sem_alterar()
    {
        var (dados, pasta, caminho) = Novo();
        using var _p = pasta;
        var id = dados.CriarDemanda("X", Fabrica.Agora).Id!;
        dados.VincularRepositorio(id, "A");
        var antes = File.ReadAllText(caminho);
        Assert.False(dados.DefinirChange(id, "A", "a b").Ok);
        Assert.False(dados.VincularRepositorio(id, "B", "a;b").Ok);
        Assert.False(dados.DefinirChange(id, "Inexistente", "x").Ok);
        Assert.Equal(antes, File.ReadAllText(caminho));
    }

    [Fact]
    public void Arquivo_com_a_change_na_demanda_vale_para_os_repositorios()
    {
        var (dados, pasta, _) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","openspecChange":"antiga","repositorios":[{"pasta":"X"},{"pasta":"Y"}]}]}""");
        using var _p = pasta;
        var d = dados.Carregar().Demandas.Single();
        Assert.All(d.Repositorios, r => Assert.Equal("antiga", r.OpenspecChange));
    }

    [Fact]
    public void Repositorio_com_change_propria_usa_a_propria()
    {
        var (dados, pasta, _) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","openspecChange":"antiga","repositorios":[{"pasta":"X","openspecChange":"propria"},{"pasta":"Y"}]}]}""");
        using var _p = pasta;
        var repos = dados.Carregar().Demandas.Single().Repositorios;
        Assert.Equal("propria", repos[0].OpenspecChange);
        Assert.Equal("antiga", repos[1].OpenspecChange);
    }

    [Fact]
    public void Change_invalida_no_repositorio_e_tratada_como_ausente()
    {
        var (dados, pasta, _) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","repositorios":[{"pasta":"X","openspecChange":"a b"}]}]}""");
        using var _p = pasta;
        Assert.Null(dados.Carregar().Demandas.Single().Repositorios.Single().OpenspecChange);
    }

    [Fact]
    public void Nenhuma_vence_a_change_antiga_da_demanda()
    {
        var (dados, pasta, _) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","openspecChange":"antiga","repositorios":[{"pasta":"X"}]}]}""");
        using var _p = pasta;
        Assert.True(dados.DefinirChange("A", "X", null).Ok);
        Assert.Null(dados.Carregar().Demandas.Single().Repositorios.Single().OpenspecChange);
    }

    [Fact]
    public void Gravar_preserva_a_change_antiga_da_demanda()
    {
        var (dados, pasta, caminho) = Novo(
            """{"demandas":[{"id":"A","titulo":"A","openspecChange":"antiga","repositorios":[{"pasta":"X"}]}]}""");
        using var _p = pasta;
        dados.DefinirChange("A", "X", "nova");
        Assert.Equal("antiga", (string?)JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!["openspecChange"]);
    }
}

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

public class SessoesNaVisaoTests
{
    private const string IdA = "aaaaaaaa-0000-0000-0000-000000000001";
    private const string IdB = "bbbbbbbb-0000-0000-0000-000000000002";
    private const string IdC = "cccccccc-0000-0000-0000-000000000003";
    private static readonly RelogioFalso Relogio = new(Fabrica.Agora);

    private static SessaoRegistrada Reg(string id, string pasta, string demanda, Agente agente = Agente.Claude)
        => new(agente, id, pasta, null, Fabrica.Agora.AddDays(-1), demanda);

    private static Demanda Dem(string id, params RepositorioCadastrado[] repos)
        => new(id, id, null, null, false, repos);

    private static Sessao Lida(string id, StatusGravado status, Agente agente = Agente.Claude)
        => Fabrica.Sessao(status, TimeSpan.FromMinutes(1), id, agente);

    [Fact]
    public void Sessoes_reune_registradas_lancamentos_e_nao_registradas()
    {
        var lanc = new LancamentoPendente("11111111-2222-3333-4444-555555555555", Agente.Codex, Fabrica.Agora.AddMinutes(-5), null);
        var sem = Dem("sem-demanda", new RepositorioCadastrado("Estoque", [Reg(IdA, "Estoque", "sem-demanda")], [lanc]));
        var trabalho = Dem("t1", new RepositorioCadastrado("Faturamento", [Reg(IdB, "Faturamento", "t1")]));
        var v = VisaoAgrupada.Montar([sem, trabalho],
            [Lida(IdA, StatusGravado.Ativa), Lida(IdB, StatusGravado.Ativa), Lida(IdC, StatusGravado.Atencao)],
            false, false, Relogio);

        Assert.Equal(["t1"], v.Demandas.Select(d => d.Demanda.Id));
        Assert.Equal(IdA, Assert.Single(v.Sessoes.Registradas).Registrada.SessionId);
        Assert.Equal("Estoque", Assert.Single(v.Sessoes.Lancamentos).Pasta);
        Assert.Equal(IdC, Assert.Single(v.Sessoes.NaoRegistradas).Sessao.SessionId);
        Assert.Equal(2, v.Sessoes.Quantidade);
    }

    [Fact]
    public void Sessoes_excluidas_somem_de_todo_lugar_inclusive_do_resumo()
    {
        var sem = Dem("sem-demanda", new RepositorioCadastrado("Estoque", [Reg(IdA, "Estoque", "sem-demanda")]));
        var trabalho = Dem("t1", new RepositorioCadastrado("F", [Reg(IdB, "F", "t1")]));
        var excluidas = new[]
        {
            new SessaoExcluida(Agente.Claude, IdA, Fabrica.Agora),
            new SessaoExcluida(Agente.Claude, IdB.ToUpperInvariant(), Fabrica.Agora),
            new SessaoExcluida(Agente.Claude, IdC, Fabrica.Agora),
        };
        var v = VisaoAgrupada.Montar([sem, trabalho],
            [Lida(IdA, StatusGravado.Atencao), Lida(IdB, StatusGravado.Atencao), Lida(IdC, StatusGravado.Atencao)],
            false, false, Relogio, excluidas);

        Assert.Empty(v.Sessoes.Registradas);
        Assert.Empty(v.Sessoes.NaoRegistradas);
        Assert.Empty(v.Demandas.Single().Cards.Single().Sessoes);
        Assert.Equal(0, v.ResumoCabecalho.PrecisaAtencao);
        Assert.Null(ProximaSessao.Escolher(v, null, false));
    }

    [Fact]
    public void Ir_para_a_proxima_chega_a_Sessoes_quando_so_ha_sessao_la()
    {
        var sem = Dem("sem-demanda", new RepositorioCadastrado("Estoque", [Reg(IdA, "Estoque", "sem-demanda")]));
        var trabalho = Dem("t1", new RepositorioCadastrado("F", [Reg(IdB, "F", "t1")]));
        var v = VisaoAgrupada.Montar([sem, trabalho],
            [Lida(IdA, StatusGravado.Aguardando), Lida(IdB, StatusGravado.Ativa)], false, false, Relogio);
        Assert.True(ProximaSessao.Escolher(v, null, false)!.EhSessoes);
    }

    [Fact]
    public void Sessao_excluida_nao_e_vinculada_por_lancamento()
    {
        var lanc = new LancamentoPendente("11111111-2222-3333-4444-555555555555", Agente.Codex, Fabrica.Agora.AddMinutes(-5), null);
        var d = Dem("t1", new RepositorioCadastrado("App", [], [lanc]));
        var lida = new Sessao(Agente.Codex, IdA, @"C:\Fontes\App", StatusGravado.Ativa, Fabrica.Agora, Fabrica.Agora.AddMinutes(-1));

        Assert.Single(VinculoAutomatico.Calcular([d], [lida], Fabrica.Agora));
        var excluidas = new[] { new SessaoExcluida(Agente.Codex, IdA, Fabrica.Agora) };
        Assert.Empty(VinculoAutomatico.Calcular([d], [lida], Fabrica.Agora, excluidas));
    }
}

public class ChangesDoRepositorioTests : IDisposable
{
    private readonly PastaTemporaria _raiz = new();

    public void Dispose() => _raiz.Dispose();

    private string Pasta(params string[] partes) => Path.Combine([_raiz.Caminho, .. partes]);

    private void Change(string repo, string nome) => Directory.CreateDirectory(Pasta(repo, "openspec", "changes", nome));

    private static void Juncao(string link, string alvo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        var p = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{alvo}\"")
            { CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
        p.WaitForExit();
        Assert.Equal(0, p.ExitCode);
    }

    [Fact]
    public void Lista_as_changes_ativas_em_ordem_sem_archive()
    {
        Change("F", "nova-api");
        Change("F", "ajuste-boleto");
        Change("F", "archive");
        Assert.Equal(["ajuste-boleto", "nova-api"], ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
    }

    [Fact]
    public void Ignora_arquivos_e_nomes_invalidos()
    {
        Change("F", "boa");
        Change("F", "nome com espaco");
        Change("F", "a;b");
        File.WriteAllText(Pasta("F", "openspec", "changes", "arquivo.md"), "x");
        Assert.Equal(["boa"], ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
    }

    [Fact]
    public void Sem_openspec_changes_a_lista_e_vazia()
    {
        Directory.CreateDirectory(Pasta("F"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "Inexistente"));
    }

    [Fact]
    public void Pasta_invalida_devolve_lista_vazia()
        => Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, ".."));

    [Fact]
    public void Limita_a_200_changes()
    {
        for (var i = 0; i < 205; i++) Change("F", $"c{i:000}");
        var lista = ChangesDoRepositorio.Listar(_raiz.Caminho, "F");
        Assert.Equal(200, lista.Count);
        Assert.Equal("c000", lista[0]);
    }

    [Fact]
    public void Entrada_que_e_juncao_nao_aparece()
    {
        Change("F", "boa");
        Directory.CreateDirectory(Pasta("Outro", "alvo"));
        Juncao(Pasta("F", "openspec", "changes", "atalho"), Pasta("Outro", "alvo"));
        Assert.Equal(["boa"], ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
    }

    [Fact]
    public void Changes_ou_openspec_que_e_juncao_recusa_tudo()
    {
        Directory.CreateDirectory(Pasta("Outro", "changes", "x"));
        Directory.CreateDirectory(Pasta("F", "openspec"));
        Juncao(Pasta("F", "openspec", "changes"), Pasta("Outro", "changes"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));

        Directory.CreateDirectory(Pasta("Alvo", "changes", "y"));
        Juncao(Pasta("G", "openspec"), Pasta("Alvo"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "G"));
    }

    [Fact]
    public void Repositorio_que_e_juncao_recusa_tudo()
    {
        Directory.CreateDirectory(Pasta("Outro", "openspec", "changes", "x"));
        Juncao(Pasta("F"), Pasta("Outro"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
    }
}

public class EscolhaDePastaTests
{
    private const string Raiz = @"C:\Fontes";

    [Theory]
    [InlineData(@"C:\Fontes\Faturamento", "Faturamento")]
    [InlineData(@"C:\Fontes\Faturamento\", "Faturamento")]
    [InlineData(@"c:\fontes\Estoque", "Estoque")]
    [InlineData(@"C:\Fontes\app.v2", "app.v2")]
    public void Aceita_pasta_direta_da_raiz_e_devolve_so_o_nome(string caminho, string nome)
    {
        var r = EscolhaDePasta.Validar(Raiz, caminho);
        Assert.True(r.Ok);
        Assert.Equal(nome, r.Pasta);
    }

    [Theory]
    [InlineData(@"C:\Projetos\Faturamento")]
    [InlineData(@"C:\Fontes\Faturamento\src")]
    [InlineData(@"C:\Fontes")]
    [InlineData(@"C:\")]
    [InlineData(@"D:\Fontes\Faturamento")]
    [InlineData(@"C:\FontesOutra\Faturamento")]
    [InlineData(@"C:\Fontes\..\Projetos\X")]
    [InlineData("")]
    [InlineData(null)]
    public void Recusa_pasta_fora_da_raiz(string? caminho)
    {
        var r = EscolhaDePasta.Validar(Raiz, caminho);
        Assert.False(r.Ok);
        Assert.Null(r.Pasta);
        Assert.Contains(@"C:\Fontes", r.Erro);
    }

    [Theory]
    [InlineData(@"C:\Fontes\com espaço")]
    [InlineData(@"C:\Fontes\a;b")]
    public void Recusa_nome_fora_da_regra_de_pasta(string caminho)
    {
        var r = EscolhaDePasta.Validar(Raiz, caminho);
        Assert.False(r.Ok);
        Assert.Contains("Nome de pasta inválido", r.Erro);
    }
}

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

public class TituloDaAbaTests
{
    [Fact]
    public void Ponto_e_virgula_no_titulo_vira_virgula()
        => Assert.Equal("Fase 1, ajustes · App", MontadorComando.Titulo("t1", "Fase 1; ajustes", Agente.Claude, "App", null));

    [Fact]
    public void Titulo_com_mais_de_40_caracteres_e_cortado()
    {
        var titulo = MontadorComando.Titulo("t1", new string('x', 60), Agente.Claude, "App", "r");
        Assert.Equal(new string('x', 40) + " · App · r", titulo);
    }

    [Fact]
    public void Titulo_do_trabalho_nao_vai_com_ponto_e_virgula_no_comando()
    {
        var repo = new RepositorioCadastrado("App", []);
        var c = MontadorComando.Novo("t1", "a;b", repo, Agente.Claude, "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10", null);
        Assert.DoesNotContain(";", c.Argumentos[4]);
    }

    [Fact]
    public void Titulo_do_trabalho_invalido_e_recusado()
    {
        var repo = new RepositorioCadastrado("App", []);
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("t1", "", repo, Agente.Codex, null, null));
        Assert.Throws<EntradaInvalidaException>(() => MontadorComando.Novo("t1", null, repo, Agente.Codex, null, null));
    }

    [Fact]
    public void Em_sessoes_o_titulo_nao_e_exigido()
    {
        var repo = new RepositorioCadastrado("App", []);
        var c = MontadorComando.Novo("sem-demanda", null, repo, Agente.Codex, null, null);
        Assert.Equal("Codex · App", c.Argumentos[4]);
    }
}
