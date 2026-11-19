using DataPilot.Core.Display;
using DataPilot.Core.Models;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Display;

public class ResumoEstruturadoTests
{
    [Theory]
    [InlineData(1, "1 precisa de atenção")]
    [InlineData(2, "2 precisam de atenção")]
    [InlineData(10, "10 precisam de atenção")]
    public void PluralDoAlertaDeAtencao(int n, string esperado)
        => Assert.Equal(esperado, new ResumoCabecalho(n, 0, 0).TextoAlerta);

    [Fact]
    public void ContagensVaoParaOAlertaNaOrdemDeUrgencia()
    {
        var r = new ResumoCabecalho(2, 4, 4);
        Assert.Equal(StatusExibido.PrecisaAtencao, r.StatusDestacado);
        Assert.Equal("2 precisam de atenção", r.TextoAlerta);
        Assert.Equal("4 aguardando você", r.TextoAguardando);
        Assert.Equal("4 executando", r.TextoExecutando);
        Assert.Equal("2 precisam de atenção, 4 aguardando você, 4 executando", r.Texto);
    }

    [Fact]
    public void SoAguardando_DestacaAguardando()
    {
        var r = new ResumoCabecalho(0, 1, 0);
        Assert.Equal(StatusExibido.Aguardando, r.StatusDestacado);
        Assert.Equal("1 aguardando você", r.TextoAlerta);
    }

    [Fact]
    public void SoExecutando_NadaPrecisaDoUsuario()
    {
        var r = new ResumoCabecalho(0, 0, 3);
        Assert.Null(r.StatusDestacado);
        Assert.Equal("Nada precisa de você agora", r.TextoAlerta);
        Assert.Equal("3 executando", r.Texto);
    }

    [Fact]
    public void ResumoNaoMudaQuaisSessoesEntram()
    {
        var relogio = new RelogioFalso(Fabrica.Agora);
        var v = RegrasExibicao.Montar(
        [
            Fabrica.Sessao(StatusGravado.Atencao, TimeSpan.FromMinutes(1), "a"),
            Fabrica.Sessao(StatusGravado.Atencao, TimeSpan.FromMinutes(2), "b"),
            Fabrica.Sessao(StatusGravado.Executando, TimeSpan.FromDays(5), "antiga"),
        ], false, relogio);
        Assert.Equal(new ResumoCabecalho(2, 0, 0), RegrasExibicao.Resumo(v));
    }

    [Theory]
    [InlineData(2, 0, "2 repositórios, nenhuma sessão")]
    [InlineData(1, 1, "1 repositório, 1 sessão")]
    [InlineData(2, 3, "2 repositórios, 3 sessões")]
    [InlineData(0, 0, "nenhum repositório, nenhuma sessão")]
    public void TextoDaDemanda(int repositorios, int sessoes, string esperado)
        => Assert.Equal(esperado, VisaoAgrupada.TextoResumo(repositorios, sessoes));
}
