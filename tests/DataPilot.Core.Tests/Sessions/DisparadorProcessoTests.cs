using DataPilot.Core.Sessions;

namespace DataPilot.Core.Tests.Sessions;

public class DisparadorProcessoTests
{
    [Fact]
    public void Executavel_inexistente_falha_com_mensagem()
    {
        var e = Assert.Throws<FalhaDisparoException>(() =>
            new DisparadorProcesso().Iniciar(new ComandoTerminal("datapilot-inexistente-xyz.exe", ["a"])));
        Assert.Contains("datapilot-inexistente-xyz.exe", e.Message);
    }
}
