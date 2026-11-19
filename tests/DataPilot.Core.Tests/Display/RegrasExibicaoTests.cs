using DataPilot.Core.Display;
using DataPilot.Core.Models;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Display;

public class RegrasExibicaoTests
{
    private static readonly RelogioFalso Relogio = new(Fabrica.Agora);
    private static StatusExibido Exibido(StatusGravado s, TimeSpan idade) =>
        RegrasExibicao.StatusExibido(Fabrica.Sessao(s, idade), Relogio);

    [Fact] public void ExecutandoHa20Min_Inativa() => Assert.Equal(StatusExibido.Inativa, Exibido(StatusGravado.Executando, TimeSpan.FromMinutes(20)));
    [Fact] public void ExecutandoHa10Min_Executando() => Assert.Equal(StatusExibido.Executando, Exibido(StatusGravado.Executando, TimeSpan.FromMinutes(10)));
    [Fact] public void ExecutandoNoLimiteDe15Min_AindaExecutando() => Assert.Equal(StatusExibido.Executando, Exibido(StatusGravado.Executando, TimeSpan.FromMinutes(15)));
    [Fact] public void ExecutandoUmSegundoAlemDe15Min_Inativa() => Assert.Equal(StatusExibido.Inativa, Exibido(StatusGravado.Executando, TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(1))));
    [Fact] public void AtivaHa20Min_Inativa() => Assert.Equal(StatusExibido.Inativa, Exibido(StatusGravado.Ativa, TimeSpan.FromMinutes(20)));
    [Fact] public void AtivaRecente_Ativa() => Assert.Equal(StatusExibido.Ativa, Exibido(StatusGravado.Ativa, TimeSpan.FromMinutes(1)));
    [Fact] public void AguardandoHa2h_Aguardando() => Assert.Equal(StatusExibido.Aguardando, Exibido(StatusGravado.Aguardando, TimeSpan.FromHours(2)));
    [Fact] public void AguardandoNoLimiteDe8h_AindaAguardando() => Assert.Equal(StatusExibido.Aguardando, Exibido(StatusGravado.Aguardando, TimeSpan.FromHours(8)));
    [Fact] public void AguardandoHa9h_Inativa() => Assert.Equal(StatusExibido.Inativa, Exibido(StatusGravado.Aguardando, TimeSpan.FromHours(9)));
    [Fact] public void AtencaoHa2h_PrecisaAtencao() => Assert.Equal(StatusExibido.PrecisaAtencao, Exibido(StatusGravado.Atencao, TimeSpan.FromHours(2)));
    [Fact] public void AtencaoHa9h_Inativa() => Assert.Equal(StatusExibido.Inativa, Exibido(StatusGravado.Atencao, TimeSpan.FromHours(9)));
    [Fact] public void EncerradaSempreEncerrada() => Assert.Equal(StatusExibido.Encerrada, Exibido(StatusGravado.Encerrada, TimeSpan.FromDays(30)));

    [Fact]
    public void DataNoFuturo_TratadaComoAgora()
    {
        var s = Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromHours(-5));
        Assert.Equal(StatusExibido.Executando, RegrasExibicao.StatusExibido(s, Relogio));
        Assert.Equal(TimeSpan.Zero, RegrasExibicao.Idade(s, Relogio));
    }

    [Fact]
    public void PassaAInativaComOTempo_SemMudarOArquivo()
    {
        var relogio = new RelogioFalso(Fabrica.Agora);
        var s = Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromMinutes(14));
        Assert.Equal(StatusExibido.Executando, RegrasExibicao.StatusExibido(s, relogio));
        relogio.Agora += TimeSpan.FromMinutes(2);
        Assert.Equal(StatusExibido.Inativa, RegrasExibicao.StatusExibido(s, relogio));
    }

    [Fact]
    public void Ordem_PorUrgenciaEDepoisMaisRecente()
    {
        var sessoes = new[]
        {
            Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.FromHours(1), "enc"),
            Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromMinutes(1), "exe"),
            Fabrica.Sessao(StatusGravado.Aguardando, TimeSpan.FromHours(3), "agu-velha"),
            Fabrica.Sessao(StatusGravado.Aguardando, TimeSpan.FromHours(1), "agu-nova"),
            Fabrica.Sessao(StatusGravado.Ativa, TimeSpan.FromMinutes(2), "ati"),
            Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromHours(1), "ina"),
            Fabrica.Sessao(StatusGravado.Atencao, TimeSpan.FromMinutes(30), "ate"),
        };
        var ids = RegrasExibicao.Montar(sessoes, false, Relogio).Select(v => v.Sessao.SessionId).ToArray();
        Assert.Equal(["ate", "agu-nova", "agu-velha", "exe", "ati", "ina", "enc"], ids);
    }

    [Fact]
    public void SessaoQuePassaParaAtencao_VaiParaOTopo()
    {
        var antes = new[] { Fabrica.Sessao(StatusGravado.Aguardando, TimeSpan.FromMinutes(1), "a"), Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromMinutes(2), "b") };
        var depois = new[] { antes[0], Fabrica.Sessao(StatusGravado.Atencao, TimeSpan.FromSeconds(1), "b") };
        Assert.Equal("b", RegrasExibicao.Montar(depois, false, Relogio)[0].Sessao.SessionId);
    }

    [Fact]
    public void Resumo_OmiteContagensZero()
    {
        var v = RegrasExibicao.Montar(
        [
            Fabrica.Sessao(StatusGravado.Atencao, TimeSpan.FromMinutes(1), "a"),
            Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromMinutes(1), "b"),
            Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromMinutes(2), "c"),
        ], false, Relogio);
        Assert.Equal("1 precisa de atenção, 2 executando", RegrasExibicao.Resumo(v).Texto);
    }

    [Fact]
    public void Resumo_IncluiAguardando()
    {
        var v = RegrasExibicao.Montar([Fabrica.Sessao(StatusGravado.Aguardando, TimeSpan.FromMinutes(1))], false, Relogio);
        Assert.Equal("1 aguardando você", RegrasExibicao.Resumo(v).Texto);
    }

    [Fact]
    public void Resumo_NadaPrecisaDoUsuario()
    {
        var v = RegrasExibicao.Montar(
        [
            Fabrica.Sessao(StatusGravado.Ativa, TimeSpan.FromMinutes(1), "a"),
            Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.FromHours(1), "b"),
        ], false, Relogio);
        Assert.Equal("Nada precisa de você agora", RegrasExibicao.Resumo(v).Texto);
        Assert.Equal("Nada precisa de você agora", RegrasExibicao.Resumo([]).Texto);
    }

    [Fact]
    public void EncerradaHa7Dias_OcultaPorPadrao_ApareceComOpcao()
    {
        var s = new[] { Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.FromDays(7)) };
        Assert.Empty(RegrasExibicao.Montar(s, false, Relogio));
        Assert.Single(RegrasExibicao.Montar(s, true, Relogio));
    }

    [Fact]
    public void EncerradaOntem_Aparece() =>
        Assert.Single(RegrasExibicao.Montar([Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.FromDays(1))], false, Relogio));

    [Fact]
    public void LimiteExatoDe3Dias_AindaAparece_UmSegundoAlemSome()
    {
        Assert.Single(RegrasExibicao.Montar([Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.FromDays(3))], false, Relogio));
        Assert.Empty(RegrasExibicao.Montar([Fabrica.Sessao(StatusGravado.Encerrada, TimeSpan.FromDays(3).Add(TimeSpan.FromSeconds(1)))], false, Relogio));
    }

    [Fact]
    public void ExecutandoParadoHaMaisDe3Dias_ViraInativaAntigaEFicaOculta()
    {
        var s = Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromDays(4));
        Assert.True(RegrasExibicao.EhAntiga(s, Relogio));
        Assert.Empty(RegrasExibicao.Montar([s], false, Relogio));
    }

    [Fact]
    public void AguardandoHa4Dias_Inativa_EOculta() =>
        Assert.Empty(RegrasExibicao.Montar([Fabrica.Sessao(StatusGravado.Aguardando, TimeSpan.FromDays(4))], false, Relogio));

    [Fact]
    public void SessoesOcultasNaoEntramNoResumo()
    {
        var v = RegrasExibicao.Montar([Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromDays(5))], false, Relogio);
        Assert.Equal("Nada precisa de você agora", RegrasExibicao.Resumo(v).Texto);
    }

    [Theory]
    [InlineData(0, "agora")]
    [InlineData(59, "agora")]
    [InlineData(60, "1 min")]
    [InlineData(2 * 60, "2 min")]
    [InlineData(59 * 60, "59 min")]
    [InlineData(3600, "1 h")]
    [InlineData(5 * 3600, "5 h")]
    [InlineData(86400, "1 d")]
    [InlineData(7 * 86400, "7 d")]
    public void TempoDecorrido(int segundos, string esperado) =>
        Assert.Equal(esperado, RegrasExibicao.TempoDecorrido(TimeSpan.FromSeconds(segundos)));

    [Theory]
    [InlineData(StatusExibido.PrecisaAtencao, "precisa de atenção")]
    [InlineData(StatusExibido.Aguardando, "aguardando você")]
    [InlineData(StatusExibido.Inativa, "inativa")]
    public void TextoDoStatus(StatusExibido s, string texto) => Assert.Equal(texto, RegrasExibicao.Texto(s));
}
