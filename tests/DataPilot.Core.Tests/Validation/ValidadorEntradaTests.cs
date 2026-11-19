using DataPilot.Core.Validation;

namespace DataPilot.Core.Tests.Validation;

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

    // ---------- Demanda ----------

    [Theory]
    [InlineData("CHG-142")]
    [InlineData("a")]
    [InlineData("sem-demanda")]
    [InlineData("v2.1_x")]
    [InlineData(".git")]
    public void Id_de_demanda_valido(string id) => Assert.True(ValidadorEntrada.IdDemandaValido(id));

    [Fact]
    public void Id_de_demanda_com_30_caracteres_e_valido_e_com_31_nao()
    {
        Assert.True(ValidadorEntrada.IdDemandaValido(new string('a', 30)));
        Assert.False(ValidadorEntrada.IdDemandaValido(new string('a', 31)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("com espaço")]
    [InlineData("a;b")]
    [InlineData("a\\b")]
    [InlineData("a\"b")]
    [InlineData("CHG\n")]
    public void Id_de_demanda_invalido(string? id) => Assert.False(ValidadorEntrada.IdDemandaValido(id));

    [Fact]
    public void Titulo_com_80_caracteres_e_valido_e_com_81_nao()
    {
        Assert.True(ValidadorEntrada.TituloValido(new string('a', 80)));
        Assert.False(ValidadorEntrada.TituloValido(new string('a', 81)));
    }

    [Fact]
    public void Titulo_aceita_acentos_espacos_e_ponto_e_virgula()
        => Assert.True(ValidadorEntrada.TituloValido("Ajuste NF-e; emissão"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("quebra\nlinha")]
    [InlineData("tab\taqui")]
    [InlineData("nulo\0")]
    public void Titulo_invalido(string? titulo) => Assert.False(ValidadorEntrada.TituloValido(titulo));

    [Fact]
    public void Titulo_com_separador_de_linha_unicode_e_invalido()
        => Assert.False(ValidadorEntrada.TituloValido("sep" + (char)0x2028 + "linha"));

    [Theory]
    [InlineData("etapa-4a-demandas", true)]
    [InlineData("..", false)]
    [InlineData("a b", false)]
    [InlineData("a;b", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void OpenspecChange_segue_a_regra_de_caracteres_do_id(string? nome, bool esperado)
        => Assert.Equal(esperado, ValidadorEntrada.OpenspecChangeValido(nome));
}
