using DataPilot.Core;

namespace DataPilot.Core.Tests;

public class LancamentoNaVisaoTests
{
    private const string L1 = ApoioVinculoAutomatico.L1;
    private const string L2 = ApoioVinculoAutomatico.L2;
    private const string L3 = ApoioVinculoAutomatico.L3;
    private const string S1 = ApoioVinculoAutomatico.S1;

    private static readonly RelogioFalso Relogio = new(Fabrica.Agora);

    private static LancamentoPendente Lanc(string id, TimeSpan idade, string? rotulo = null, Agente agente = Agente.Codex)
        => new(id, agente, Fabrica.Agora - idade, rotulo);

    private static Demanda Dem(string id, bool arquivada, IReadOnlyList<SessaoRegistrada> sessoes, params LancamentoPendente[] lancamentos)
        => new(id, id, null, null, arquivada, [new RepositorioCadastrado("Faturamento", sessoes, lancamentos)]);

    private static Visao Montar(Demanda[] demandas, Sessao[]? lidas = null, bool arquivadas = false)
        => VisaoAgrupada.Montar(demandas, lidas ?? [], arquivadas, false, Relogio);

    // ---------- Lançamento pendente no card ----------

    [Fact]
    public void Codex_recem_aberto_aparece_no_card_com_agente_rotulo_e_tempo()
    {
        var v = Montar([Dem("a", false, [], Lanc(L1, TimeSpan.FromMinutes(5), "ajuste"))]);

        var card = Assert.Single(Assert.Single(v.Demandas).Cards);
        var l = Assert.Single(card.Lancamentos);
        Assert.Equal(L1, l.Lancamento.Id);
        Assert.Equal(Agente.Codex, l.Lancamento.Agente);
        Assert.Equal("ajuste", l.Lancamento.Rotulo);
        Assert.Equal("5 min", l.Tempo);
        Assert.Empty(card.Sessoes);
    }

    [Fact]
    public void Dois_lancamentos_no_mesmo_card_vem_do_mais_recente_para_o_mais_antigo()
    {
        var v = Montar([Dem("a", false, [], Lanc(L1, TimeSpan.FromHours(3)), Lanc(L2, TimeSpan.FromMinutes(1)), Lanc(L3, TimeSpan.FromMinutes(30)))]);
        var ids = v.Demandas.Single().Cards.Single().Lancamentos.Select(l => l.Lancamento.Id);
        Assert.Equal([L2, L3, L1], ids);
    }

    [Fact]
    public void Lancamentos_com_a_mesma_data_ficam_em_ordem_estavel_pelo_identificador()
    {
        var v = Montar([Dem("a", false, [], Lanc(L2, TimeSpan.FromMinutes(1)), Lanc(L1, TimeSpan.FromMinutes(1)))]);
        Assert.Equal([L1, L2], v.Demandas.Single().Cards.Single().Lancamentos.Select(l => l.Lancamento.Id));
    }

    [Fact]
    public void Lancamento_vencido_nao_aparece_mas_o_de_exatamente_24_horas_aparece()
    {
        var v = Montar([Dem("a", false, [], Lanc(L1, TimeSpan.FromHours(24)), Lanc(L2, TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1)))]);
        Assert.Equal([L1], v.Demandas.Single().Cards.Single().Lancamentos.Select(l => l.Lancamento.Id));
    }

    [Fact]
    public void Lancamento_de_demanda_arquivada_nao_aparece_mesmo_mostrando_arquivadas()
    {
        var v = Montar([Dem("a", true, [], Lanc(L1, TimeSpan.FromMinutes(5)))], arquivadas: true);
        var card = Assert.Single(Assert.Single(v.Demandas).Cards);
        Assert.Empty(card.Lancamentos);
    }

    [Fact]
    public void Lancamento_aparece_depois_das_sessoes_registradas_sem_mudar_a_ordem_delas()
    {
        var sessao = new SessaoRegistrada(Agente.Claude, S1, "Faturamento", null, Fabrica.Agora - TimeSpan.FromDays(1), "a");
        var lida = Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromMinutes(1), S1);
        var v = Montar([Dem("a", false, [sessao], Lanc(L1, TimeSpan.FromMinutes(1)))], [lida]);

        var card = v.Demandas.Single().Cards.Single();
        Assert.Equal(S1, Assert.Single(card.Sessoes).Registrada.SessionId);
        Assert.Single(card.Lancamentos);
    }

    [Fact]
    public void Lancamento_com_data_no_futuro_conta_como_agora()
    {
        var v = Montar([Dem("a", false, [], Lanc(L1, TimeSpan.FromMinutes(-10)))]);
        Assert.Equal("agora", v.Demandas.Single().Cards.Single().Lancamentos.Single().Tempo);
    }

    [Fact]
    public void Lancamento_nao_vira_sessao_do_card_nem_das_nao_vinculadas()
    {
        var v = Montar([Dem("a", false, [], Lanc(L1, TimeSpan.FromMinutes(1)))]);
        Assert.Empty(v.Demandas.Single().Cards.Single().Sessoes);
        Assert.Empty(v.Sessoes.NaoRegistradas);
    }

    // ---------- Lançamento pendente fora das contagens ----------

    [Fact]
    public void Demanda_so_com_lancamento_tem_um_repositorio_zero_sessoes_e_sem_status()
    {
        var v = Montar([Dem("a", false, [], Lanc(L1, TimeSpan.FromMinutes(1)))]);
        var d = Assert.Single(v.Demandas);
        Assert.Equal(1, d.QuantidadeRepositorios);
        Assert.Equal(0, d.QuantidadeSessoes);
        Assert.Null(d.MaisUrgente);
        Assert.Equal("1 repositório, nenhuma sessão", d.Resumo);
        Assert.False(d.TemSessaoEmAndamento);
        Assert.Equal("Nada precisa de você agora", v.ResumoCabecalho.Texto);
    }

    [Fact]
    public void Lancamento_nao_muda_o_status_mais_urgente_nem_a_contagem_de_uma_demanda_com_sessao()
    {
        var sessao = new SessaoRegistrada(Agente.Claude, S1, "Faturamento", null, Fabrica.Agora - TimeSpan.FromDays(1), "a");
        var lida = Fabrica.Sessao(StatusGravado.Aguardando, TimeSpan.FromMinutes(1), S1);
        var sem = Montar([Dem("a", false, [sessao])], [lida]).Demandas.Single();
        var com = Montar([Dem("a", false, [sessao], Lanc(L1, TimeSpan.FromMinutes(1)), Lanc(L2, TimeSpan.FromMinutes(2)))], [lida]).Demandas.Single();

        Assert.Equal(sem.QuantidadeSessoes, com.QuantidadeSessoes);
        Assert.Equal(sem.MaisUrgente, com.MaisUrgente);
        Assert.Equal(sem.Resumo, com.Resumo);
    }

    [Fact]
    public void Lancamento_nao_muda_a_ordem_das_demandas_nem_o_resumo_do_cabecalho()
    {
        var sa = new SessaoRegistrada(Agente.Claude, S1, "Faturamento", null, Fabrica.Agora - TimeSpan.FromDays(1), "a");
        var sb = new SessaoRegistrada(Agente.Claude, ApoioVinculoAutomatico.S2, "Faturamento", null, Fabrica.Agora - TimeSpan.FromDays(1), "b");
        var lidas = new[]
        {
            Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromMinutes(1), S1),
            Fabrica.Sessao(StatusGravado.Atencao, TimeSpan.FromMinutes(1), ApoioVinculoAutomatico.S2),
        };

        var antes = Montar([Dem("a", false, [sa]), Dem("b", false, [sb])], lidas);
        // Um lançamento recente na demanda "a" não pode fazê-la subir na lista.
        var depois = Montar([Dem("a", false, [sa], Lanc(L1, TimeSpan.Zero)), Dem("b", false, [sb])], lidas);

        Assert.Equal(antes.Demandas.Select(d => d.Demanda.Id), depois.Demandas.Select(d => d.Demanda.Id));
        Assert.Equal(["b", "a"], depois.Demandas.Select(d => d.Demanda.Id));
        Assert.Equal(antes.ResumoCabecalho, depois.ResumoCabecalho);
    }

    [Fact]
    public void Demanda_so_com_lancamento_continua_depois_das_que_tem_status()
    {
        var sb = new SessaoRegistrada(Agente.Claude, ApoioVinculoAutomatico.S2, "Faturamento", null, Fabrica.Agora - TimeSpan.FromDays(1), "b");
        var lida = Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.FromDays(1), ApoioVinculoAutomatico.S2);
        var v = Montar([Dem("a", false, [], Lanc(L1, TimeSpan.Zero)), Dem("b", false, [sb])], [lida]);
        Assert.Equal(["b", "a"], v.Demandas.Select(d => d.Demanda.Id));
    }
}

public class CancelarLancamentoTests
{
    private const string L1 = ApoioVinculoAutomatico.L1;
    private const string L2 = ApoioVinculoAutomatico.L2;
    private const string S1 = ApoioVinculoAutomatico.S1;
    private static readonly DateTimeOffset Base = ApoioVinculoAutomatico.Base;

    private static (DadosPainel Dados, PastaTemporaria Pasta) ComDoisLancamentos()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, "um", Base);
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L2, "dois", Base.AddMinutes(1));
        return (dados, pasta);
    }

    [Fact]
    public void Cancelar_remove_o_lancamento_do_arquivo_de_dados()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, Base);

        Assert.True(dados.CancelarLancamento(L1).Ok);
        Assert.Empty(dados.Carregar().Demandas.Single().Repositorios.Single().Lancamentos);
    }

    [Fact]
    public void Cancelar_um_de_dois_deixa_o_outro_pendente_e_no_card()
    {
        var (dados, pasta) = ComDoisLancamentos();
        using var _ = pasta;

        Assert.True(dados.CancelarLancamento(L1).Ok);

        var estado = dados.Carregar();
        Assert.Equal(L2, Assert.Single(estado.Demandas.Single().Repositorios.Single().Lancamentos).Id);
        var v = VisaoAgrupada.Montar(estado.Demandas, [], false, false, new RelogioFalso(Base.AddMinutes(5)));
        Assert.Equal(L2, Assert.Single(v.Demandas.Single().Cards.Single().Lancamentos).Lancamento.Id);
    }

    [Fact]
    public void Cancelar_lancamento_que_nao_existe_nao_altera_o_arquivo()
    {
        var (dados, pasta) = ComDoisLancamentos();
        using var _ = pasta;
        var antes = File.ReadAllBytes(dados.Caminho);
        var escrita = File.GetLastWriteTimeUtc(dados.Caminho);

        var r = dados.CancelarLancamento("dddddddd-4444-4444-8444-dddddddddddd");

        Assert.False(r.Ok);
        Assert.Equal(antes, File.ReadAllBytes(dados.Caminho));
        Assert.Equal(escrita, File.GetLastWriteTimeUtc(dados.Caminho));
    }

    [Fact]
    public void Cancelar_lancamento_ja_usado_por_outra_janela_nao_altera_a_sessao_vinculada()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, "ajuste", Base);
        Assert.True(dados.VincularPorLancamento(PedidoVinculo.Criar(ApoioVinculoAutomatico.Codex(S1, Base))!, "CHG-142", "Faturamento", L1, Base.AddMinutes(1)).Ok);
        var antes = File.ReadAllBytes(dados.Caminho);

        Assert.False(dados.CancelarLancamento(L1).Ok);

        Assert.Equal(antes, File.ReadAllBytes(dados.Caminho));
        Assert.Equal(S1, dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes.Single().SessionId);
    }

    [Fact]
    public void Sessao_que_surge_depois_do_cancelamento_nao_e_vinculada()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, Base);
        Assert.True(dados.CancelarLancamento(L1).Ok);

        var sessao = ApoioVinculoAutomatico.Codex(S1, Base.AddMinutes(2));
        var relogio = new RelogioFalso(Base.AddMinutes(3));
        var estado = dados.Carregar();
        var gravados = VinculoAutomatico.Aplicar(dados, estado, [sessao], relogio);

        Assert.Equal(0, gravados);
        Assert.Empty(dados.Carregar().Demandas.Single().Repositorios.Single().Sessoes);
        var v = VisaoAgrupada.Montar(dados.Carregar().Demandas, [sessao], false, false, relogio);
        Assert.Equal(S1, Assert.Single(v.Sessoes.NaoRegistradas).Sessao.SessionId);
    }
}
