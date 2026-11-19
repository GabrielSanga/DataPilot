using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Persistence;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Linking;

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
