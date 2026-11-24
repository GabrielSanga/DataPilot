using DataPilot.Core.Validation;

namespace DataPilot.Core.Tests.Validation;

public class UsuarioSshTests
{
    [Theory]
    [InlineData("ia")]
    [InlineData("agente-ia")]
    [InlineData("_a.b-c")]
    [InlineData("12345678901234567890")]
    public void Valido(string usuario) => Assert.True(ValidadorEntrada.UsuarioSshValido(usuario));

    [Theory]
    [InlineData("-oProxyCommand=calc")]
    [InlineData(".x")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("123456789012345678901")]
    [InlineData("a b")]
    [InlineData("a@b")]
    [InlineData("ia\n")]
    public void Invalido(string? usuario) => Assert.False(ValidadorEntrada.UsuarioSshValido(usuario));
}
