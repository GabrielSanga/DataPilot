namespace DataPilot.Core.Models;

/// <summary>Como o painel abre os agentes: pelo SSH local (com o usuário escolhido) ou direto.</summary>
public sealed record Configuracoes(bool UsarSsh, string UsuarioSsh)
{
    public static Configuracoes Padrao { get; } = new(true, Constantes.UsuarioSshPadrao);
}
