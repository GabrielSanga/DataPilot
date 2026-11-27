using System.Text.Json.Nodes;
using DataPilot.Core.Display;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Tests.Helpers;
using DataPilot.Core.Tests.Persistence;

namespace DataPilot.Core.Tests.Linking;

public class VinculoSessaoTests
{
    private const string IdCodex = "0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10";
    private const string IdOutra = "11111111-2222-3333-4444-555555555555";
    private const string TextoReconhecivel = "SEGREDO-DA-PASTA-DE-STATUS";

    private static readonly RelogioFalso Relogio = new(Fabrica.Agora);

    private static Sessao Lida(string id, Agente agente = Agente.Codex, string cwd = @"C:\Fontes\Faturamento",
        StatusGravado status = StatusGravado.Ativa, TimeSpan? idade = null)
        => new(agente, id, cwd, status, Fabrica.Agora - (idade ?? TimeSpan.FromMinutes(1)));

    private static PedidoVinculo Pedido(string id = IdCodex, Agente agente = Agente.Codex)
        => PedidoVinculo.Criar(Lida(id, agente))!;

    /// <summary>Arquivo com a demanda CHG-142 (repositório Faturamento) e CHG-150 (arquivada).</summary>
    private static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) ComDemanda()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo();
        Assert.True(dados.CriarDemanda("CHG-142", "Ajuste NF-e", null, Fabrica.Agora).Ok);
        Assert.True(dados.VincularRepositorio("CHG-142", "Faturamento").Ok);
        Assert.True(dados.CriarDemanda("CHG-150", "Arquivada", null, Fabrica.Agora).Ok);
        Assert.True(dados.ArquivarDemanda("CHG-150").Ok);
        return (dados, pasta, caminho);
    }

    // ---------- Somente GUID canônico pode ser vinculado ----------

    [Fact]
    public void Pedido_e_criado_para_guid_canonico_com_agente_e_identificador()
    {
        var pedido = PedidoVinculo.Criar(Lida(IdCodex, Agente.Codex));
        Assert.NotNull(pedido);
        Assert.Equal(Agente.Codex, pedido.Agente);
        Assert.Equal(IdCodex, pedido.SessionId);
    }

    [Theory]
    [InlineData("ex-atencao")]
    [InlineData("")]
    [InlineData("0b6f1c2e5d1a4c8e9f3a2b7d4e6a8c10")]
    [InlineData("{0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10}")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c1")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10 ")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10\n")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c1g")]
    public void Pedido_e_recusado_para_identificador_fora_do_formato(string id)
    {
        Assert.Null(PedidoVinculo.Criar(Lida(id)));
        Assert.Null(PedidoVinculo.Criar(Registrada(id)));
    }

    [Fact]
    public void Pode_vincular_guid_canonico()
    {
        Assert.NotNull(PedidoVinculo.Criar(Lida(IdCodex)));
        var pedido = PedidoVinculo.Criar(Registrada(IdCodex));
        Assert.Equal((Agente.Codex, IdCodex), (pedido!.Agente, pedido.SessionId));
    }

    private static SessaoRegistrada Registrada(string id)
        => new(Agente.Codex, id, "Faturamento", null, DateTimeOffset.UtcNow, "sem-demanda");

    // ---------- Vincular sessão não vinculada / Rastro do vínculo ----------

    [Fact]
    public void Vincular_grava_agente_identificador_rotulo_e_datas()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _ = pasta;
        var quando = Fabrica.Agora.AddMinutes(5);

        var r = dados.VincularSessao(Pedido(), "CHG-142", "Faturamento", "revisão", quando);

        Assert.True(r.Ok, r.Mensagem);
        var sessao = Assert.Single(dados.Carregar().Demandas.Single(d => d.Id == "CHG-142").Repositorios.Single().Sessoes);
        Assert.Equal(Agente.Codex, sessao.Agente);
        Assert.Equal(IdCodex, sessao.SessionId);
        Assert.Equal("revisão", sessao.Rotulo);
        Assert.Equal(quando, sessao.CriadaEm);

        var no = JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!["repositorios"]![0]!["sessoes"]![0]!;
        Assert.Equal(quando, DateTimeOffset.Parse(no["vinculadaEm"]!.GetValue<string>()));
    }

    [Fact]
    public void Vincular_sem_rotulo_nao_grava_rotulo()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _ = pasta;
        Assert.True(dados.VincularSessao(Pedido(), "CHG-142", "Faturamento", null, Fabrica.Agora).Ok);
        var no = JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!["repositorios"]![0]!["sessoes"]![0]!.AsObject();
        Assert.False(no.ContainsKey("rotulo"));
        Assert.True(no.ContainsKey("vinculadaEm"));
    }

    [Fact]
    public void Vincular_a_repositorio_novo_vincula_o_repositorio_a_demanda()
    {
        var (dados, pasta, _) = ComDemanda();
        using var _p = pasta;

        var r = dados.VincularSessao(Pedido(), "CHG-142", "Estoque", null, Fabrica.Agora);

        Assert.True(r.Ok, r.Mensagem);
        var demanda = dados.Carregar().Demandas.Single(d => d.Id == "CHG-142");
        Assert.Equal(["Faturamento", "Estoque"], demanda.Repositorios.Select(x => x.Pasta));
        Assert.Equal(IdCodex, Assert.Single(demanda.Repositorios[1].Sessoes).SessionId);
    }

    [Fact]
    public void Vincular_a_repositorio_novo_invalido_nao_grava_nada()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _p = pasta;
        var antes = File.ReadAllText(caminho);
        Assert.False(dados.VincularSessao(Pedido(), "CHG-142", "..", null, Fabrica.Agora).Ok);
        Assert.False(dados.VincularSessao(Pedido(), "CHG-142", "a;b", null, Fabrica.Agora).Ok);
        Assert.Equal(antes, File.ReadAllText(caminho));
    }

    [Fact]
    public void Vincular_com_rotulo_invalido_e_recusado()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _p = pasta;
        var antes = File.ReadAllText(caminho);
        var r = dados.VincularSessao(Pedido(), "CHG-142", "Faturamento", "a;b", Fabrica.Agora);
        Assert.False(r.Ok);
        Assert.Equal(antes, File.ReadAllText(caminho));
    }

    // ---------- Sessão já registrada não é vinculada de novo ----------

    [Fact]
    public void Vincular_sessao_ja_registrada_e_recusado_sem_alterar_o_arquivo()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _p = pasta;
        Assert.True(dados.VincularSessao(Pedido(), "CHG-142", "Faturamento", null, Fabrica.Agora).Ok);
        var antes = File.ReadAllText(caminho);

        var r = dados.VincularSessao(Pedido(), "CHG-142", "Faturamento", null, Fabrica.Agora.AddHours(1));

        Assert.False(r.Ok);
        Assert.Contains("já está registrada", r.Mensagem);
        Assert.Equal(antes, File.ReadAllText(caminho));
    }

    [Fact]
    public void Vincular_sessao_registrada_em_outra_demanda_nao_cria_repositorio_novo()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _p = pasta;
        Assert.True(dados.CriarDemanda("CHG-160", "Outra", null, Fabrica.Agora).Ok);
        Assert.True(dados.VincularSessao(Pedido(), "CHG-142", "Faturamento", null, Fabrica.Agora).Ok);
        var antes = File.ReadAllText(caminho);

        var r = dados.VincularSessao(Pedido(), "CHG-160", "Estoque", null, Fabrica.Agora);

        Assert.False(r.Ok);
        Assert.Equal(antes, File.ReadAllText(caminho));
    }

    // ---------- demanda arquivada, inexistente, arquivo ilegível ----------

    [Fact]
    public void Vincular_a_demanda_arquivada_e_recusado()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _p = pasta;
        var antes = File.ReadAllText(caminho);
        var r = dados.VincularSessao(Pedido(), "CHG-150", "Faturamento", null, Fabrica.Agora);
        Assert.False(r.Ok);
        Assert.Contains("arquivado", r.Mensagem);
        Assert.Equal(antes, File.ReadAllText(caminho));
    }

    [Fact]
    public void Vincular_a_demanda_inexistente_e_recusado()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _p = pasta;
        var antes = File.ReadAllText(caminho);
        var r = dados.VincularSessao(Pedido(), "NAO-EXISTE", "Faturamento", null, Fabrica.Agora);
        Assert.False(r.Ok);
        Assert.Contains("não existe", r.Mensagem);
        Assert.Equal(antes, File.ReadAllText(caminho));
    }

    [Fact]
    public void Vincular_com_arquivo_ilegivel_nao_sobrescreve()
    {
        var (dados, pasta, caminho) = DadosPainelTests.Novo("isto nao e json");
        using var _p = pasta;
        var r = dados.VincularSessao(Pedido(), "CHG-142", "Faturamento", null, Fabrica.Agora);
        Assert.False(r.Ok);
        Assert.Equal("isto nao e json", File.ReadAllText(caminho));
    }

    // ---------- Somente identificador e agente cruzam ----------

    [Fact]
    public void Nada_alem_de_identificador_e_agente_vem_do_arquivo_de_status()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _p = pasta;
        var lida = new Sessao(Agente.Codex, IdCodex, @"C:\Fontes\" + TextoReconhecivel, StatusGravado.Atencao,
            new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero));

        var r = dados.VincularSessao(PedidoVinculo.Criar(lida)!, "CHG-142", "Faturamento", null, Fabrica.Agora);

        Assert.True(r.Ok, r.Mensagem);
        var texto = File.ReadAllText(caminho);
        Assert.DoesNotContain(TextoReconhecivel, texto);
        Assert.DoesNotContain("Atencao", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("atencao", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("2001", texto);
        Assert.DoesNotContain("evento", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cwd", texto, StringComparison.OrdinalIgnoreCase);

        var no = JsonNode.Parse(texto)!["demandas"]![0]!["repositorios"]![0]!["sessoes"]![0]!.AsObject();
        Assert.Equal(["agente", "sessionId", "execucao", "criadaEm", "vinculadaEm"], no.Select(p => p.Key));
    }

    [Fact]
    public void Pedido_so_carrega_agente_e_identificador()
    {
        var propriedades = typeof(PedidoVinculo).GetProperties().Select(p => p.Name).Order().ToArray();
        Assert.Equal(["Agente", "SessionId"], propriedades);
        Assert.Empty(typeof(PedidoVinculo).GetConstructors());
    }

    // ---------- Rastro: sessões abertas pelo painel não têm vinculadaEm ----------

    [Fact]
    public void Sessao_registrada_pelo_painel_nao_recebe_vinculadaEm()
    {
        var (dados, pasta, caminho) = ComDemanda();
        using var _p = pasta;
        Assert.True(dados.RegistrarSessao("CHG-142", "Faturamento", Agente.Claude, IdOutra, null, Fabrica.Agora).Ok);
        var no = JsonNode.Parse(File.ReadAllText(caminho))!["demandas"]![0]!["repositorios"]![0]!["sessoes"]![0]!.AsObject();
        Assert.False(no.ContainsKey("vinculadaEm"));
    }

    // ---------- Desfazer o vínculo ----------

    [Fact]
    public void Remover_registro_de_sessao_vinculada_a_devolve_a_nao_vinculadas()
    {
        var (dados, pasta, _) = ComDemanda();
        using var _p = pasta;
        var lida = Lida(IdCodex);
        Assert.True(dados.VincularSessao(PedidoVinculo.Criar(lida)!, "CHG-142", "Faturamento", null, Fabrica.Agora).Ok);

        var vinculada = VisaoAgrupada.Montar(dados.Carregar().Demandas, [lida], false, false, Relogio);
        Assert.Empty(vinculada.Sessoes.NaoRegistradas);
        Assert.Single(vinculada.Demandas.Single().Cards.Single().Sessoes);

        Assert.True(dados.DesfazerRegistro(IdCodex).Ok);

        var depois = VisaoAgrupada.Montar(dados.Carregar().Demandas, [lida], false, false, Relogio);
        Assert.Equal(IdCodex, Assert.Single(depois.Sessoes.NaoRegistradas).Sessao.SessionId);
        Assert.Empty(depois.Demandas.Single().Cards.Single().Sessoes);
        Assert.NotNull(PedidoVinculo.Criar(lida));
    }

    // ---------- Sem sugestões no card: não vinculadas ficam só na entrada própria ----------

    private static Demanda Dem(string id, bool arquivada, params string[] pastas)
        => new(id, id, null, null, arquivada, pastas.Select(p => new RepositorioCadastrado(p, [])).ToList());

    private static Visao Montar(Demanda[] demandas, Sessao[] lidas, bool antigas = false)
        => VisaoAgrupada.Montar(demandas, lidas, true, antigas, Relogio);

    [Fact]
    public void Sessao_nao_vinculada_de_mesma_pasta_nao_aparece_em_nenhum_card()
    {
        var v = Montar([Dem("CHG-142", false, "Faturamento", "Estoque"), Dem("CHG-150", false, "Faturamento")],
            [Lida(IdCodex, cwd: @"C:\Fontes\Faturamento")]);

        Assert.All(v.Demandas.SelectMany(d => d.Cards), c => Assert.Empty(c.Sessoes));
        Assert.Equal(IdCodex, Assert.Single(v.Sessoes.NaoRegistradas).Sessao.SessionId);
    }

    [Fact]
    public void Sessao_vinculada_aparece_so_no_card_em_que_foi_registrada()
    {
        var registrada = new SessaoRegistrada(Agente.Codex, IdCodex, "Faturamento", null, Fabrica.Agora, "a");
        var a = new Demanda("a", "a", null, null, false, [new RepositorioCadastrado("Faturamento", [registrada])]);
        var v = Montar([a, Dem("b", false, "Faturamento")], [Lida(IdCodex)]);

        Assert.Single(v.Demandas.Single(d => d.Demanda.Id == "a").Cards.Single().Sessoes);
        Assert.Empty(v.Demandas.Single(d => d.Demanda.Id == "b").Cards.Single().Sessoes);
        Assert.Empty(v.Sessoes.NaoRegistradas);
    }

    // ---------- Coincidência de pasta (pré-seleção do diálogo e aviso) ----------

    [Theory]
    [InlineData(@"C:\Fontes\faturamento")]
    [InlineData(@"C:\Fontes\FATURAMENTO\")]
    [InlineData("/home/ia/Faturamento")]
    public void Coincidencia_ignora_maiusculas_barra_final_e_separador(string cwd)
        => Assert.True(RegrasVinculo.PastaCoincide(cwd, "Faturamento"));

    [Theory]
    [InlineData(@"C:\Fontes\Faturamento2")]
    [InlineData(@"C:\Faturamento\Outra")]
    [InlineData(@"C:\Fontes\Fatura")]
    public void Pasta_de_nome_diferente_nao_coincide(string cwd)
        => Assert.False(RegrasVinculo.PastaCoincide(cwd, "Faturamento"));

    [Theory]
    [InlineData(@"D:\Projetos\Faturamento", true)]
    [InlineData(@"d:\projetos\FATURAMENTO\", true)]
    [InlineData(@"C:\Fontes\Faturamento", false)]
    [InlineData(@"D:\Projetos\Faturamento\src", false)]
    public void Repositorio_fora_da_raiz_coincide_pelo_caminho_completo(string cwd, bool coincide)
        => Assert.Equal(coincide, RegrasVinculo.PastaCoincide(cwd, @"D:\Projetos\Faturamento"));

    // ---------- Aviso de pasta divergente ----------

    [Fact]
    public void Repositorio_diferente_da_pasta_informada_avisa()
        => Assert.True(RegrasVinculo.AvisarPastaDivergente(@"C:\Fontes\Faturamento", "Estoque"));

    [Theory]
    [InlineData(@"C:\Fontes\Faturamento")]
    [InlineData(@"C:\Fontes\faturamento\")]
    public void Repositorio_igual_a_pasta_informada_nao_avisa(string cwd)
        => Assert.False(RegrasVinculo.AvisarPastaDivergente(cwd, "Faturamento"));
}
