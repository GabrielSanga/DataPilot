using DataPilot.Core;

namespace DataPilot.Core.Tests;

public class ValidadorStatusTests
{
    private static bool Ler(string json, string nome, out Sessao? s) => ValidadorStatus.TentarLer(json, nome, out s);

    [Fact]
    public void ArquivoValido_GeraSessaoComOsCampos()
    {
        Assert.True(Ler(Fabrica.Json(), "s1", out var s));
        Assert.Equal(Agente.Claude, s!.Agente);
        Assert.Equal("s1", s.SessionId);
        Assert.Equal(@"C:\Fontes\App", s.Cwd);
        Assert.Equal(StatusGravado.Executando, s.Status);
        Assert.Equal(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.FromHours(-3)).AddTicks(1234567), s.Atualizado);
    }

    [Theory]
    [InlineData("nao e json")]
    [InlineData("")]
    [InlineData("[1,2]")]
    [InlineData("\"texto\"")]
    public void ConteudoQueNaoEObjetoJson_Descartado(string conteudo) => Assert.False(Ler(conteudo, "s1", out _));

    [Fact]
    public void StatusDesconhecido_Descartado() => Assert.False(Ler(Fabrica.Json(status: "dormindo"), "s1", out _));

    [Fact]
    public void AgenteDesconhecido_Descartado() => Assert.False(Ler(Fabrica.Json(agente: "gemini"), "s1", out _));

    [Fact]
    public void IdentificadorDiferenteDoNomeDoArquivo_Descartado() => Assert.False(Ler(Fabrica.Json(id: "s1"), "outro", out _));

    [Theory]
    [InlineData("../x")]
    [InlineData("a b")]
    public void IdentificadorForaDoPadrao_Descartado(string id) => Assert.False(Ler(Fabrica.Json(id: id), id, out _));

    [Fact]
    public void IdentificadorComMaisDe100Caracteres_Descartado()
    {
        var id = new string('a', 101);
        Assert.False(Ler(Fabrica.Json(id: id), id, out _));
    }

    [Theory]
    [InlineData("agente")]
    [InlineData("session_id")]
    [InlineData("status")]
    [InlineData("atualizado")]
    public void CampoObrigatorioAusente_Descartado(string campo)
    {
        var obj = System.Text.Json.Nodes.JsonNode.Parse(Fabrica.Json())!.AsObject();
        obj.Remove(campo);
        Assert.False(Ler(obj.ToJsonString(), "s1", out _));
    }

    [Fact]
    public void CampoAdicional_Ignorado() => Assert.True(Ler(Fabrica.Json(extra: ",\"novo\":{\"a\":1}"), "s1", out _));

    [Theory]
    [InlineData("2026-11-14T09:00:00")]
    [InlineData("2026-11-14")]
    [InlineData("ontem")]
    public void DataSemFusoOuInvalida_Descartado(string data) => Assert.False(Ler(Fabrica.Json(atualizado: data), "s1", out _));

    [Theory]
    [InlineData("2026-11-14T09:00:00Z")]
    [InlineData("2026-11-14T09:00:00+02:00")]
    public void DataComFuso_Aceita(string data) => Assert.True(Ler(Fabrica.Json(atualizado: data), "s1", out _));

    [Fact]
    public void JsonMuitoProfundo_Descartado()
    {
        var json = new string('[', 200) + new string(']', 200);
        Assert.False(Ler(json, "s1", out _));
    }
}
