using DataPilot.Core.Display;

namespace DataPilot.Core.Tests.Display;

public class TextoExibicaoTests
{
    [Fact]
    public void TextoLongo_CortadoEm260ComIndicacao()
    {
        var r = TextoExibicao.Limpar(new string('a', 400));
        Assert.Equal(261, r.Length);
        Assert.EndsWith("…", r);
    }

    [Fact]
    public void TextoDe260_NaoEhCortado()
    {
        var t = new string('a', 260);
        Assert.Equal(t, TextoExibicao.Limpar(t));
    }

    [Fact]
    public void CaracteresDeControle_Removidos()
    {
        Assert.Equal("C:\\a\\b", TextoExibicao.Limpar("C:\\a\r\n\\b\t\0\u001b"));
    }

    [Fact]
    public void CaracteresDeDirecaoInvisiveis_Removidos()
    {
        Assert.Equal("ab", TextoExibicao.Limpar("a\u202Eb\u200B"));
    }

    [Fact]
    public void CorteNaoPartePareDeSubstitutos()
    {
        var r = TextoExibicao.Limpar(new string('a', 259) + "😀😀");
        Assert.Equal(new string('a', 259) + "…", r);
    }

    [Theory]
    [InlineData("/workspace/Faturamento", "Faturamento")]
    [InlineData(@"C:\Fontes\Grupo\Faturamento\", "Faturamento")]
    [InlineData("/home/x/proj/", "proj")]
    [InlineData("proj", "proj")]
    [InlineData("", "")]
    [InlineData(@"C:\", "C:")]
    [InlineData(@"\", @"\")]
    public void NomePasta_UltimoSegmento(string cwd, string esperado) => Assert.Equal(esperado, TextoExibicao.NomePasta(cwd));

    [Fact]
    public void NomePasta_Nula() => Assert.Equal("", TextoExibicao.NomePasta(null));
}
