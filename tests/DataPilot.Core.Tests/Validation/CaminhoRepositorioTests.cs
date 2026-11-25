using DataPilot.Core.Validation;

namespace DataPilot.Core.Tests.Validation;

public class CaminhoRepositorioTests
{
    [Theory]
    [InlineData(@"D:\Projetos\Faturamento")]
    [InlineData(@"C:\Users\Gabriel Sanga\código & cia;v2")]
    [InlineData(@"c:\x")]
    public void Aceita_caminho_completo_de_unidade_local(string caminho)
    {
        Assert.True(ValidadorEntrada.CaminhoValido(caminho));
        Assert.True(ValidadorEntrada.RepositorioValido(caminho));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"C:\")]
    [InlineData(@"C:\Projetos\")]
    [InlineData(@"Projetos\Faturamento")]
    [InlineData(@"\\servidor\pasta")]
    [InlineData(@"C:\Projetos\..\Windows")]
    [InlineData(@"C:\Projetos\.\X")]
    [InlineData(@"C:\Projetos\\X")]
    [InlineData(@"C:/Projetos/X")]
    [InlineData(@"C:\Pro*jetos")]
    [InlineData("C:\\Projetos\nX")]
    [InlineData(@"C:\Projetos\a:b")]
    public void Recusa_caminho_que_nao_e_completo_ou_normalizado(string? caminho)
        => Assert.False(ValidadorEntrada.CaminhoValido(caminho));

    [Fact]
    public void Recusa_caminho_acima_de_260_caracteres()
        => Assert.False(ValidadorEntrada.CaminhoValido(@"C:\" + new string('a', 258)));

    [Fact]
    public void Nome_da_raiz_continua_valendo_como_repositorio()
        => Assert.True(ValidadorEntrada.RepositorioValido("Faturamento"));
}
