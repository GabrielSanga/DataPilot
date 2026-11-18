using DataPilot.Core;

namespace DataPilot.Core.Tests;

public class VisaoAgrupadaTests
{
    private const string IdA = "aaaaaaaa-0000-0000-0000-000000000001";
    private const string IdB = "aaaaaaaa-0000-0000-0000-000000000002";
    private const string IdC = "aaaaaaaa-0000-0000-0000-000000000003";
    private const string IdD = "aaaaaaaa-0000-0000-0000-000000000004";
    private const string IdE = "aaaaaaaa-0000-0000-0000-000000000005";

    private static readonly RelogioFalso Relogio = new(Fabrica.Agora);

    private static SessaoRegistrada Reg(string id, string pasta, string demanda, Agente agente = Agente.Claude, string? rotulo = null, TimeSpan? criada = null)
        => new(agente, id, pasta, rotulo, Fabrica.Agora - (criada ?? TimeSpan.FromDays(1)), demanda);

    private static Demanda Dem(string id, bool arquivada = false, params RepositorioCadastrado[] repos)
        => new(id, id.ToUpperInvariant(), null, null, arquivada, repos);

    private static RepositorioCadastrado Repo(string pasta, params SessaoRegistrada[] sessoes) => new(pasta, sessoes);

    private static Sessao Lida(string id, StatusGravado status, TimeSpan idade, Agente agente = Agente.Claude)
        => Fabrica.Sessao(status, idade, id, agente);

    private static Visao Montar(Demanda[] demandas, Sessao[] lidas, bool arquivadas = false, bool antigas = false)
        => VisaoAgrupada.Montar(demandas, lidas, arquivadas, antigas, Relogio);

    // ---------- Tela agrupada por demanda ----------

    [Fact]
    public void Demanda_com_dois_repositorios_e_os_dois_agentes_mostra_dois_cards()
    {
        var d = Dem("chg-1", false,
            Repo("Faturamento", Reg(IdA, "Faturamento", "chg-1")),
            Repo("Estoque", Reg(IdB, "Estoque", "chg-1", Agente.Codex)));
        var v = Montar([d], [Lida(IdA, StatusGravado.Executando, TimeSpan.FromMinutes(1)),
                             Lida(IdB, StatusGravado.Ativa, TimeSpan.FromMinutes(1), Agente.Codex)]);

        var item = Assert.Single(v.Demandas);
        Assert.Equal(["Faturamento", "Estoque"], item.Cards.Select(c => c.Pasta));
        Assert.Equal(IdA, Assert.Single(item.Cards[0].Sessoes).Registrada.SessionId);
        Assert.Equal(IdB, Assert.Single(item.Cards[1].Sessoes).Registrada.SessionId);
        Assert.Empty(v.NaoVinculadas);
    }

    [Fact]
    public void Mesma_pasta_em_duas_demandas_mostra_cada_sessao_so_na_sua()
    {
        var a = Dem("a", false, Repo("Faturamento", Reg(IdA, "Faturamento", "a")));
        var b = Dem("b", false, Repo("Faturamento", Reg(IdB, "Faturamento", "b")));
        var v = Montar([a, b], [Lida(IdA, StatusGravado.Ativa, TimeSpan.Zero), Lida(IdB, StatusGravado.Ativa, TimeSpan.Zero)]);
        Assert.Equal([IdA], v.Demandas.Single(x => x.Demanda.Id == "a").Cards.Single().Sessoes.Select(s => s.Registrada.SessionId));
        Assert.Equal([IdB], v.Demandas.Single(x => x.Demanda.Id == "b").Cards.Single().Sessoes.Select(s => s.Registrada.SessionId));
    }

    [Fact]
    public void Demanda_sem_repositorios_aparece_com_zero_cards()
    {
        var v = Montar([Dem("a")], []);
        var item = Assert.Single(v.Demandas);
        Assert.Empty(item.Cards);
        Assert.Equal(0, item.QuantidadeRepositorios);
    }

    [Fact]
    public void Nenhuma_demanda_resulta_em_visao_vazia()
    {
        var v = Montar([], []);
        Assert.Empty(v.Demandas);
        Assert.Empty(v.NaoVinculadas);
        Assert.Equal("Nada precisa de você agora", v.ResumoCabecalho.Texto);
    }

    [Fact]
    public void Sem_demanda_sem_repositorios_fica_oculta_e_com_repositorios_aparece()
    {
        Assert.Empty(Montar([Dem("sem-demanda")], []).Demandas);
        Assert.Single(Montar([Dem("sem-demanda", false, Repo("X"))], []).Demandas);
    }

    // ---------- Sessões registradas sempre visíveis ----------

    [Fact]
    public void Sessao_registrada_encerrada_ha_uma_semana_continua_no_card()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a")));
        var v = Montar([d], [Lida(IdA, StatusGravado.Encerrada, TimeSpan.FromDays(7))], antigas: false);
        var s = Assert.Single(v.Demandas.Single().Cards.Single().Sessoes);
        Assert.Equal(StatusExibido.Encerrada, s.Status);
        Assert.Equal("7 d", s.Tempo);
    }

    [Fact]
    public void Sessao_registrada_sem_arquivo_de_status_aparece_como_sem_status()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a")));
        var v = Montar([d], []);
        var s = Assert.Single(v.Demandas.Single().Cards.Single().Sessoes);
        Assert.Equal(StatusExibido.SemStatus, s.Status);
        Assert.Null(s.Lida);
        Assert.Equal("sem status", RegrasExibicao.Texto(s.Status));
        Assert.Equal(IdA, s.Registrada.SessionId);
    }

    [Fact]
    public void Sem_status_vem_depois_das_demais_no_card()
    {
        var d = Dem("a", false, Repo("X",
            Reg(IdA, "X", "a"), Reg(IdB, "X", "a"), Reg(IdC, "X", "a")));
        var v = Montar([d], [Lida(IdB, StatusGravado.Encerrada, TimeSpan.FromDays(10)), Lida(IdC, StatusGravado.Atencao, TimeSpan.FromHours(1))]);
        Assert.Equal([IdC, IdB, IdA], v.Demandas.Single().Cards.Single().Sessoes.Select(s => s.Registrada.SessionId));
    }

    [Fact]
    public void Correspondencia_e_por_identificador_e_agente()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a", Agente.Claude)));
        var v = Montar([d], [Lida(IdA, StatusGravado.Ativa, TimeSpan.Zero, Agente.Codex)]);
        Assert.Equal(StatusExibido.SemStatus, v.Demandas.Single().Cards.Single().Sessoes.Single().Status);
        Assert.Equal(IdA, Assert.Single(v.NaoVinculadas).Sessao.SessionId);
    }

    [Fact]
    public void Pasta_de_trabalho_do_status_nao_influencia_o_card()
    {
        var d = Dem("a", false, Repo("Faturamento", Reg(IdA, "Faturamento", "a")));
        var lida = Fabrica.Sessao(StatusGravado.Ativa, TimeSpan.Zero, IdA, cwd: @"C:\Windows");
        var v = Montar([d], [lida]);
        var s = v.Demandas.Single().Cards.Single().Sessoes.Single();
        Assert.Equal("Faturamento", s.Registrada.Pasta);
        Assert.Equal(@"C:\Windows", s.Lida!.Cwd);
    }

    [Fact]
    public void Identificador_do_status_em_caixa_diferente_corresponde()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a")));
        var v = Montar([d], [Lida(IdA.ToUpperInvariant(), StatusGravado.Ativa, TimeSpan.Zero)]);
        Assert.Equal(StatusExibido.Ativa, v.Demandas.Single().Cards.Single().Sessoes.Single().Status);
        Assert.Empty(v.NaoVinculadas);
    }

    // ---------- Entrada "Não vinculadas" ----------

    [Fact]
    public void Sessao_do_codex_nao_registrada_vai_para_nao_vinculadas()
    {
        var v = Montar([Dem("a", false, Repo("X"))], [Lida(IdA, StatusGravado.Ativa, TimeSpan.Zero, Agente.Codex)]);
        Assert.Equal(IdA, Assert.Single(v.NaoVinculadas).Sessao.SessionId);
    }

    [Fact]
    public void Sessao_criada_por_clear_vai_para_nao_vinculadas_e_a_original_fica_na_demanda()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a")));
        var v = Montar([d], [Lida(IdA, StatusGravado.Encerrada, TimeSpan.FromMinutes(1)), Lida(IdB, StatusGravado.Ativa, TimeSpan.Zero)]);
        Assert.Equal([IdB], v.NaoVinculadas.Select(s => s.Sessao.SessionId));
        Assert.Single(v.Demandas.Single().Cards.Single().Sessoes);
    }

    [Fact]
    public void Nenhuma_sessao_nao_vinculada_resulta_em_lista_vazia()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a")));
        Assert.Empty(Montar([d], [Lida(IdA, StatusGravado.Ativa, TimeSpan.Zero)]).NaoVinculadas);
    }

    [Fact]
    public void Sessao_registrada_em_demanda_arquivada_oculta_nao_e_nao_vinculada()
    {
        var d = Dem("a", true, Repo("X", Reg(IdA, "X", "a")));
        var v = Montar([d], [Lida(IdA, StatusGravado.Ativa, TimeSpan.Zero)], arquivadas: false);
        Assert.Empty(v.Demandas);
        Assert.Empty(v.NaoVinculadas);
    }

    // ---------- Resumo e ordem das demandas ----------

    [Fact]
    public void Resumo_indica_o_status_mais_urgente_e_as_contagens()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a"), Reg(IdB, "X", "a")), Repo("Y"));
        var v = Montar([d], [Lida(IdA, StatusGravado.Executando, TimeSpan.FromMinutes(1)), Lida(IdB, StatusGravado.Atencao, TimeSpan.FromMinutes(5))]);
        var item = v.Demandas.Single();
        Assert.Equal(StatusExibido.PrecisaAtencao, item.MaisUrgente);
        Assert.Equal(2, item.QuantidadeRepositorios);
        Assert.Equal(2, item.QuantidadeSessoes);
        Assert.Equal("2 repositórios, 2 sessões", item.Resumo);
    }

    [Fact]
    public void Demanda_sem_sessoes_indica_um_repositorio_zero_sessoes_sem_status()
    {
        var item = Montar([Dem("a", false, Repo("X"))], []).Demandas.Single();
        Assert.Null(item.MaisUrgente);
        Assert.Equal("1 repositório, nenhuma sessão", item.Resumo);
    }

    [Fact]
    public void Demanda_so_com_sessoes_sem_arquivo_de_status_resume_como_sem_status()
    {
        var item = Montar([Dem("a", false, Repo("X", Reg(IdA, "X", "a")))], []).Demandas.Single();
        Assert.Null(item.MaisUrgente);
        Assert.Equal("1 repositório, 1 sessão", item.Resumo);
    }

    [Fact]
    public void Demandas_ordenadas_por_urgencia_e_depois_pela_atualizacao_mais_recente()
    {
        var calma = Dem("calma", false, Repo("X", Reg(IdA, "X", "calma")));
        var executando = Dem("exec", false, Repo("X", Reg(IdB, "X", "exec")));
        var atencao = Dem("atencao", false, Repo("X", Reg(IdC, "X", "atencao")));
        var executando2 = Dem("exec-nova", false, Repo("X", Reg(IdD, "X", "exec-nova")));
        var v = Montar([calma, executando, atencao, executando2],
        [
            Lida(IdA, StatusGravado.Encerrada, TimeSpan.FromHours(1)),
            Lida(IdB, StatusGravado.Executando, TimeSpan.FromMinutes(10)),
            Lida(IdC, StatusGravado.Atencao, TimeSpan.FromHours(3)),
            Lida(IdD, StatusGravado.Executando, TimeSpan.FromMinutes(2)),
        ]);
        Assert.Equal(["atencao", "exec-nova", "exec", "calma"], v.Demandas.Select(d => d.Demanda.Id));
    }

    [Fact]
    public void Demandas_sem_sessoes_com_status_vem_depois_e_mantem_a_ordem_do_arquivo()
    {
        var vazia1 = Dem("vazia1", false, Repo("X"));
        var comStatus = Dem("com", false, Repo("X", Reg(IdA, "X", "com")));
        var vazia2 = Dem("vazia2");
        var v = Montar([vazia1, comStatus, vazia2], [Lida(IdA, StatusGravado.Encerrada, TimeSpan.FromDays(1))]);
        Assert.Equal(["com", "vazia1", "vazia2"], v.Demandas.Select(d => d.Demanda.Id));
    }

    [Fact]
    public void Arquivadas_aparecem_so_com_a_opcao_e_por_ultimo()
    {
        var arq = Dem("arq", true, Repo("X", Reg(IdA, "X", "arq")));
        var normal = Dem("normal", false, Repo("X"));
        var lidas = new[] { Lida(IdA, StatusGravado.Atencao, TimeSpan.FromMinutes(1)) };

        Assert.Equal(["normal"], Montar([arq, normal], lidas).Demandas.Select(d => d.Demanda.Id));
        var v = Montar([arq, normal], lidas, arquivadas: true);
        Assert.Equal(["normal", "arq"], v.Demandas.Select(d => d.Demanda.Id));
        Assert.True(v.Demandas[1].Demanda.Arquivada);
    }

    // ---------- Ordem por urgência dentro dos cards ----------

    [Fact]
    public void Sessoes_do_card_em_ordem_de_urgencia_e_depois_mais_recente()
    {
        var d = Dem("a", false, Repo("X",
            Reg(IdA, "X", "a"), Reg(IdB, "X", "a"), Reg(IdC, "X", "a"), Reg(IdD, "X", "a"), Reg(IdE, "X", "a")));
        var v = Montar([d],
        [
            Lida(IdA, StatusGravado.Encerrada, TimeSpan.FromHours(1)),
            Lida(IdB, StatusGravado.Aguardando, TimeSpan.FromHours(3)),
            Lida(IdC, StatusGravado.Aguardando, TimeSpan.FromHours(1)),
            Lida(IdD, StatusGravado.Executando, TimeSpan.FromMinutes(1)),
            Lida(IdE, StatusGravado.Atencao, TimeSpan.FromHours(2)),
        ]);
        Assert.Equal([IdE, IdC, IdB, IdD, IdA], v.Demandas.Single().Cards.Single().Sessoes.Select(s => s.Registrada.SessionId));
    }

    [Fact]
    public void Sessao_que_passa_a_pedir_atencao_vai_para_o_topo()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a"), Reg(IdB, "X", "a")));
        var antes = Montar([d], [Lida(IdA, StatusGravado.Aguardando, TimeSpan.FromMinutes(1)), Lida(IdB, StatusGravado.Executando, TimeSpan.FromMinutes(2))]);
        var depois = Montar([d], [Lida(IdA, StatusGravado.Aguardando, TimeSpan.FromMinutes(1)), Lida(IdB, StatusGravado.Atencao, TimeSpan.FromSeconds(1))]);
        Assert.Equal(IdA, antes.Demandas.Single().Cards.Single().Sessoes[0].Registrada.SessionId);
        Assert.Equal(IdB, depois.Demandas.Single().Cards.Single().Sessoes[0].Registrada.SessionId);
    }

    [Fact]
    public void Nao_vinculadas_seguem_a_mesma_ordem_de_urgencia()
    {
        var v = Montar([], [Lida(IdA, StatusGravado.Encerrada, TimeSpan.FromHours(1)), Lida(IdB, StatusGravado.Atencao, TimeSpan.FromHours(1))]);
        Assert.Equal([IdB, IdA], v.NaoVinculadas.Select(s => s.Sessao.SessionId));
    }

    // ---------- Antigas e resumo do cabeçalho ----------

    [Fact]
    public void Antigas_so_sao_ocultadas_entre_as_nao_vinculadas()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a")));
        var lidas = new[]
        {
            Lida(IdA, StatusGravado.Encerrada, TimeSpan.FromDays(7)),
            Lida(IdB, StatusGravado.Encerrada, TimeSpan.FromDays(7)),
            Lida(IdC, StatusGravado.Encerrada, TimeSpan.FromDays(1)),
        };

        var oculta = Montar([d], lidas, antigas: false);
        Assert.Single(oculta.Demandas.Single().Cards.Single().Sessoes);
        Assert.Equal([IdC], oculta.NaoVinculadas.Select(s => s.Sessao.SessionId));

        var todas = Montar([d], lidas, antigas: true);
        Assert.Equal(2, todas.NaoVinculadas.Count);
        Assert.Single(todas.Demandas.Single().Cards.Single().Sessoes);
    }

    [Fact]
    public void Sessao_executando_parada_ha_4_dias_vira_inativa_antiga_entre_as_nao_vinculadas()
    {
        var v = Montar([], [Lida(IdA, StatusGravado.Executando, TimeSpan.FromDays(4))]);
        Assert.Empty(v.NaoVinculadas);
    }

    [Fact]
    public void Resumo_do_cabecalho_conta_demandas_listadas_e_nao_vinculadas_visiveis()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a"), Reg(IdB, "X", "a")));
        var v = Montar([d],
        [
            Lida(IdA, StatusGravado.Atencao, TimeSpan.FromMinutes(1)),
            Lida(IdB, StatusGravado.Executando, TimeSpan.FromMinutes(1)),
            Lida(IdC, StatusGravado.Executando, TimeSpan.FromMinutes(1)),
            Lida(IdD, StatusGravado.Executando, TimeSpan.FromDays(5)),
        ]);
        Assert.Equal("1 precisa de atenção, 2 executando", v.ResumoCabecalho.Texto);
    }

    [Fact]
    public void Sem_status_nao_entra_no_resumo_do_cabecalho()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a")));
        Assert.Equal("Nada precisa de você agora", Montar([d], []).ResumoCabecalho.Texto);
    }

    [Fact]
    public void Sessoes_de_demanda_arquivada_oculta_nao_entram_no_resumo_do_cabecalho()
    {
        var d = Dem("a", true, Repo("X", Reg(IdA, "X", "a")));
        var lidas = new[] { Lida(IdA, StatusGravado.Atencao, TimeSpan.FromMinutes(1)) };
        Assert.Equal("Nada precisa de você agora", Montar([d], lidas).ResumoCabecalho.Texto);
        Assert.Equal("1 precisa de atenção", Montar([d], lidas, arquivadas: true).ResumoCabecalho.Texto);
    }

    // ---------- Sessão em andamento (aviso ao arquivar) ----------

    [Theory]
    [InlineData(StatusGravado.Executando, 1, true)]
    [InlineData(StatusGravado.Atencao, 1, true)]
    [InlineData(StatusGravado.Encerrada, 1, false)]
    [InlineData(StatusGravado.Executando, 1200, false)] // 20 h parado: inativa
    public void Tem_sessao_em_andamento(StatusGravado status, int minutos, bool esperado)
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a")));
        var v = Montar([d], [Lida(IdA, status, TimeSpan.FromMinutes(minutos))]);
        Assert.Equal(esperado, v.Demandas.Single().TemSessaoEmAndamento);
    }

    [Fact]
    public void Sessao_sem_status_nao_conta_como_em_andamento()
    {
        var d = Dem("a", false, Repo("X", Reg(IdA, "X", "a")));
        Assert.False(Montar([d], []).Demandas.Single().TemSessaoEmAndamento);
    }

    // ---------- Regras de ação da demanda ----------

    [Fact]
    public void Sem_demanda_nao_oferece_editar_nem_arquivar()
    {
        var d = Dem("sem-demanda", false, Repo("X"));
        Assert.False(RegrasDemanda.PodeEditar(d));
        Assert.False(RegrasDemanda.PodeArquivar(d));
        Assert.True(RegrasDemanda.PodeVincularRepositorio(d));
        Assert.True(RegrasDemanda.PodeAbrirNovaSessao(d));
    }

    [Fact]
    public void Demanda_arquivada_nao_oferece_nova_sessao_nem_vincular()
    {
        var d = Dem("a", true, Repo("X"));
        Assert.False(RegrasDemanda.PodeAbrirNovaSessao(d));
        Assert.False(RegrasDemanda.PodeVincularRepositorio(d));
        Assert.True(RegrasDemanda.PodeDesarquivar(d));
        Assert.False(RegrasDemanda.PodeArquivar(d));
    }

    [Fact]
    public void Demanda_sem_repositorios_nao_oferece_nova_sessao()
        => Assert.False(RegrasDemanda.PodeAbrirNovaSessao(Dem("a")));
}
