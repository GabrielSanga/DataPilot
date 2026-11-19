namespace DataPilot.Core.Tests.Helpers;

public static class Espera
{
    public static bool Ate(Func<bool> condicao, TimeSpan limite)
    {
        var fim = DateTime.UtcNow + limite;
        while (DateTime.UtcNow < fim)
        {
            if (condicao()) return true;
            Thread.Sleep(25);
        }
        return condicao();
    }
}
