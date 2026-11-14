namespace DataPilot.Core;

/// <summary>Comando a iniciar: executável e lista de argumentos já separados. Nunca passa por shell.</summary>
public sealed record ComandoTerminal(string Executavel, IReadOnlyList<string> Argumentos);

public sealed class EntradaInvalidaException(string mensagem) : Exception(mensagem);

/// <summary>
/// Monta o comando <c>wt</c> + <c>ssh</c> + <c>abrir.ps1</c> como função pura. Só aceita tipos do arquivo de dados
/// do painel (nunca <see cref="Sessao"/>, que vem da pasta de status) e valida de novo cada valor.
/// </summary>
public static class MontadorComando
{
    public static ComandoTerminal Novo(RepositorioCadastrado repo, Agente agente, string? sessionId, string? rotulo)
    {
        if (agente == Agente.Claude && sessionId is null)
            throw new EntradaInvalidaException("A sessão nova do Claude precisa de um identificador.");
        if (agente == Agente.Codex && sessionId is not null)
            throw new EntradaInvalidaException("A sessão nova do Codex não recebe identificador.");
        return Montar(agente, repo.Pasta, sessionId, "novo", rotulo);
    }

    public static ComandoTerminal Retomar(SessaoRegistrada sessao)
        => Montar(sessao.Agente, sessao.Pasta, sessao.SessionId, "retomar", sessao.Rotulo);

    public static string Titulo(Agente agente, string pasta, string? rotulo)
    {
        var nome = agente == Agente.Claude ? "Claude" : "Codex";
        var titulo = $"{nome} · {pasta}";
        return rotulo is null ? titulo : $"{titulo} · {rotulo}";
    }

    private static ComandoTerminal Montar(Agente agente, string pasta, string? sessionId, string modo, string? rotulo)
    {
        if (!Enum.IsDefined(agente)) throw new EntradaInvalidaException("Agente inválido.");
        if (!ValidadorEntrada.PastaValida(pasta))
            throw new EntradaInvalidaException($"Pasta inválida: {TextoExibicao.Limpar(pasta)}");
        if (sessionId is not null && !ValidadorEntrada.IdentificadorValido(sessionId))
            throw new EntradaInvalidaException("Identificador de sessão inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo))
            throw new EntradaInvalidaException("Rótulo inválido: use até 40 caracteres, sem \";\" nem caracteres de controle.");

        var args = new List<string>
        {
            "-w", Constantes.JanelaTerminal, "nt",
            "--title", Titulo(agente, pasta, rotulo), "--suppressApplicationTitle",
            "ssh", "-t", Constantes.DestinoSsh,
            "powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Constantes.ScriptAbrir,
            "-Agente", ValidadorEntrada.NomeAgente(agente),
            "-Repo", pasta,
        };
        if (sessionId is not null) { args.Add("-Sessao"); args.Add(sessionId); }
        args.Add("-Modo");
        args.Add(modo);
        return new ComandoTerminal(Constantes.ExecutavelTerminal, args);
    }
}
