namespace DataPilot.Core;

/// <summary>Regras que ligam o status exibido às ações do painel.</summary>
public static class Correspondencia
{
    /// <summary>Avisa antes de retomar ou arquivar quando a sessão não está encerrada, inativa nem sem status.</summary>
    public static bool ExigeConfirmacao(StatusExibido status)
        => status is not (StatusExibido.Encerrada or StatusExibido.Inativa or StatusExibido.SemStatus);
}
