using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Persistence;

/// <summary>Cenários de "Ambiente de execução nos registros" (dados-do-painel).</summary>
public class ExecucaoNosRegistrosTests
{
    private const string Id = DadosPainelTests.Id1;
    private const string L1 = DadosPainelTests.Id2;

    private static (DadosPainel Dados, PastaTemporaria Pasta, string Caminho) Com(string sessao, string lancamento)
        => DadosPainelTests.Novo($$"""
            {"demandas":[{"id":"CHG-142","titulo":"Ajuste","arquivada":false,"repositorios":[
              {"pasta":"Faturamento",
               "sessoes":[{"agente":"claude","sessionId":"{{Id}}"{{sessao}}}],
               "lancamentos":[{"id":"{{L1}}","agente":"codex","criadoEm":"2026-11-14T12:00:00+00:00"{{lancamento}}}]}]}]}
            """);

    private static RepositorioCadastrado Repo(DadosPainel d) => d.Carregar().Demandas.Single(x => x.Id == "CHG-142").Repositorios.Single();

    private static PedidoVinculo Pedido(string id, Agente agente = Agente.Codex)
        => PedidoVinculo.Criar(Fabrica.Sessao(StatusGravado.Ativa, TimeSpan.Zero, id, agente))!;

    [Fact]
    public void Registros_sem_o_campo_valem_windows()
    {
        var (dados, pasta, _) = Com("", "");
        using var _ = pasta;
        Assert.Equal(Execucao.Windows, Repo(dados).Sessoes.Single().Execucao);
        Assert.Equal(Execucao.Windows, Repo(dados).Lancamentos.Single().Execucao);
    }

    [Fact]
    public void Registros_do_container_sao_lidos()
    {
        var (dados, pasta, _) = Com(",\"execucao\":\"container\"", ",\"execucao\":\"container\"");
        using var _ = pasta;
        Assert.Equal(Execucao.Container, Repo(dados).Sessoes.Single().Execucao);
        Assert.Equal(Execucao.Container, Repo(dados).Lancamentos.Single().Execucao);
    }

    [Theory]
    [InlineData(",\"execucao\":\"wsl\"")]
    [InlineData(",\"execucao\":7")]
    public void Ambiente_desconhecido_desconsidera_o_registro(string campo)
    {
        var (dados, pasta, _) = Com(campo, campo);
        using var _ = pasta;
        Assert.Empty(Repo(dados).Sessoes);
        Assert.Empty(Repo(dados).Lancamentos);
    }

    [Fact]
    public void Grava_o_campo_na_sessao_e_no_lancamento()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        Assert.True(dados.RegistrarSessao("CHG-142", "Faturamento", Agente.Claude, Id, null, Fabrica.Agora, Execucao.Container).Ok);
        Assert.True(dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, Fabrica.Agora, Execucao.Windows).Ok);
        var texto = File.ReadAllText(dados.Caminho);
        Assert.Contains("\"execucao\": \"container\"", texto);
        Assert.Equal(Execucao.Container, Repo(dados).Sessoes.Single().Execucao);
        Assert.Equal(Execucao.Windows, Repo(dados).Lancamentos.Single().Execucao);
    }

    [Theory]
    [InlineData(Execucao.Container)]
    [InlineData(Execucao.Windows)]
    public void Vinculo_por_lancamento_copia_o_ambiente_dele(Execucao execucao)
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        // As configuracoes em uso sao o contrario, para provar que o valor vem do lancamento.
        dados.SalvarConfiguracoes(Configuracoes.Padrao with { Execucao = execucao == Execucao.Windows ? Execucao.Container : Execucao.Windows });
        dados.RegistrarLancamento("CHG-142", "Faturamento", Agente.Codex, L1, null, Fabrica.Agora, execucao);

        Assert.True(dados.VincularPorLancamento(Pedido(Id), "CHG-142", "Faturamento", L1, Fabrica.Agora).Ok);

        Assert.Equal(execucao, Repo(dados).Sessoes.Single().Execucao);
    }

    [Fact]
    public void Vinculo_manual_de_sessao_ja_registrada_mantem_o_ambiente()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.RegistrarRetomada(Pedido(Id), "Faturamento", Fabrica.Agora, Execucao.Windows);
        dados.SalvarConfiguracoes(Configuracoes.Padrao);

        Assert.True(dados.VincularSessao(Pedido(Id), "CHG-142", "Faturamento", null, Fabrica.Agora).Ok);

        Assert.Equal(Execucao.Windows, Repo(dados).Sessoes.Single().Execucao);
    }

    [Fact]
    public void Vinculo_manual_de_sessao_nao_registrada_usa_o_ambiente_em_uso()
    {
        var (dados, pasta) = ApoioVinculoAutomatico.NovoDados("CHG-142");
        using var _ = pasta;
        dados.SalvarConfiguracoes(Configuracoes.Padrao);

        Assert.True(dados.VincularSessao(Pedido(Id), "CHG-142", "Faturamento", null, Fabrica.Agora).Ok);

        Assert.Equal(Execucao.Container, Repo(dados).Sessoes.Single().Execucao);
    }
}
