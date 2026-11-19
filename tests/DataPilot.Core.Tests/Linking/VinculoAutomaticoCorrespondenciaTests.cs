using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Linking;

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
