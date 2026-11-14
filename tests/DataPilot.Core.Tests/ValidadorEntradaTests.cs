using DataPilot.Core;

namespace DataPilot.Core.Tests;

public class ValidadorEntradaTests
{
    [Theory]
    [InlineData("Faturamento")]
    [InlineData("app_v2.1-x")]
    [InlineData(".git")]
    [InlineData("a..b")]
    public void Pasta_valida(string pasta) => Assert.True(ValidadorEntrada.PastaValida(pasta));

    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("...")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("com espaço")]
    [InlineData("a\\b")]
    [InlineData("a/b")]
    [InlineData("a;b")]
    [InlineData("..\\Windows")]
    [InlineData("nome\n")]
    [InlineData("açúcar")]
    public void Pasta_invalida(string? pasta) => Assert.False(ValidadorEntrada.PastaValida(pasta));

    [Theory]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10")]
    [InlineData("0B6F1C2E-5D1A-4C8E-9F3A-2B7D4E6A8C10")]
    public void Identificador_valido(string id) => Assert.True(ValidadorEntrada.IdentificadorValido(id));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0b6f1c2e5d1a4c8e9f3a2b7d4e6a8c10")]
    [InlineData("0b6f1c2-e5d1a-4c8e-9f3a-2b7d4e6a8c10")]
    [InlineData("------------------------------------")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c1")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10\n")]
    [InlineData("{0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8c10}")]
    [InlineData("0b6f1c2e-5d1a-4c8e-9f3a-2b7d4e6a8cxx")]
    public void Identificador_invalido(string? id) => Assert.False(ValidadorEntrada.IdentificadorValido(id));

    [Fact]
    public void Rotulo_valido_com_acentos_e_limite()
    {
        Assert.True(ValidadorEntrada.RotuloValido("implementação"));
        Assert.True(ValidadorEntrada.RotuloValido(new string('a', 40)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a;b")]
    [InlineData("quebra\nlinha")]
    [InlineData("tab\taqui")]
    [InlineData("nulo\0")]
    public void Rotulo_invalido(string? rotulo) => Assert.False(ValidadorEntrada.RotuloValido(rotulo));

    [Fact]
    public void Rotulo_com_41_caracteres_e_invalido() => Assert.False(ValidadorEntrada.RotuloValido(new string('a', 41)));

    [Fact]
    public void Rotulo_com_separador_de_linha_unicode_e_invalido()
        => Assert.False(ValidadorEntrada.RotuloValido("sep" + (char)0x2028 + "linha"));
}
