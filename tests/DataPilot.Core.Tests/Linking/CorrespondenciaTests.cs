using DataPilot.Core.Linking;
using DataPilot.Core.Models;

namespace DataPilot.Core.Tests.Linking;

public class CorrespondenciaTests
{
    [Theory]
    [InlineData(StatusExibido.PrecisaAtencao, true)]
    [InlineData(StatusExibido.Aguardando, true)]
    [InlineData(StatusExibido.Executando, true)]
    [InlineData(StatusExibido.Ativa, true)]
    [InlineData(StatusExibido.Inativa, false)]
    [InlineData(StatusExibido.Encerrada, false)]
    [InlineData(StatusExibido.SemStatus, false)]
    public void Confirmacao_conforme_o_status(StatusExibido status, bool esperado)
        => Assert.Equal(esperado, Correspondencia.ExigeConfirmacao(status));
}
