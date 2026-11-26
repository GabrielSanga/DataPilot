using DataPilot.Core.Display;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Sessions;

/// <summary>Comando a iniciar: executável e lista de argumentos já separados. Nunca passa por shell.</summary>
public sealed record ComandoTerminal(string Executavel, IReadOnlyList<string> Argumentos);

public sealed class EntradaInvalidaException(string mensagem) : Exception(mensagem);

/// <summary>
/// Monta o comando <c>wt</c> + <c>ssh</c> + <c>abrir.ps1</c> como função pura. Só aceita tipos do arquivo de dados
/// do painel (nunca <see cref="Sessao"/>, que vem da pasta de status) e valida de novo cada valor.
/// </summary>
public static class MontadorComando
{
    public static ComandoTerminal Novo(string demandaId, string? tituloDemanda, RepositorioCadastrado repo, Agente agente, string? sessionId, string? rotulo, Configuracoes? opcoes = null)
    {
        if (agente == Agente.Claude && sessionId is null)
            throw new EntradaInvalidaException("A sessão nova do Claude precisa de um identificador.");
        if (agente == Agente.Codex && sessionId is not null)
            throw new EntradaInvalidaException("A sessão nova do Codex não recebe identificador.");
        return Montar(demandaId, tituloDemanda, agente, repo.Pasta, sessionId, "novo", rotulo, opcoes);
    }

    public static ComandoTerminal Retomar(SessaoRegistrada sessao, Configuracoes? opcoes = null)
        => Montar(sessao.DemandaId, sessao.DemandaTitulo, sessao.Agente, sessao.Pasta, sessao.SessionId, "retomar", sessao.Rotulo, opcoes);

    public const int TamanhoMaximoTituloNaAba = 40;

    /// <summary>
    /// <c>título · pasta [· rótulo]</c>. O título do trabalho entra com até 40 caracteres e cada <c>;</c> trocado por
    /// <c>,</c> (o Windows Terminal trata <c>;</c> como separador de comandos); em "Sessões" (<c>sem-demanda</c>) o nome
    /// do agente ocupa o lugar do título.
    /// </summary>
    public static string Titulo(string demandaId, string? tituloDemanda, Agente agente, string pasta, string? rotulo)
    {
        var prefixo = RegrasDemanda.EhImplicita(demandaId)
            ? Agentes.Nome(agente)
            : TituloNaAba(tituloDemanda);
        var titulo = $"{prefixo} · {TextoExibicao.NomePasta(pasta).Replace(';', ',')}";
        return rotulo is null ? titulo : $"{titulo} · {rotulo}";
    }

    /// <summary>Título do trabalho como aparece na aba: até 40 caracteres, sem <c>;</c>.</summary>
    public static string TituloNaAba(string? titulo)
    {
        var texto = (titulo ?? "").Replace(';', ',');
        if (texto.Length <= TamanhoMaximoTituloNaAba) return texto;
        var corte = TamanhoMaximoTituloNaAba;
        if (char.IsHighSurrogate(texto[corte - 1])) corte--;
        return texto[..corte];
    }

    private static ComandoTerminal Montar(string demandaId, string? tituloDemanda, Agente agente, string pasta, string? sessionId, string modo, string? rotulo, Configuracoes? opcoes)
    {
        opcoes ??= Configuracoes.Padrao;
        if (!Enum.IsDefined(agente)) throw new EntradaInvalidaException("Agente inválido.");
        if (!ValidadorEntrada.IdDemandaValido(demandaId))
            throw new EntradaInvalidaException($"Identificador de trabalho inválido: {TextoExibicao.Limpar(demandaId)}");
        if (!RegrasDemanda.EhImplicita(demandaId) && !ValidadorEntrada.TituloValido(tituloDemanda))
            throw new EntradaInvalidaException("Título do trabalho inválido.");
        if (!ValidadorEntrada.RepositorioValido(pasta))
            throw new EntradaInvalidaException(ValidadorEntrada.MensagemRepositorio);
        if (sessionId is not null && !ValidadorEntrada.IdentificadorValido(sessionId))
            throw new EntradaInvalidaException("Identificador de sessão inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo))
            throw new EntradaInvalidaException(ValidadorEntrada.MensagemRotulo);

        if (opcoes.UsarSsh && !ValidadorEntrada.UsuarioSshValido(opcoes.UsuarioSsh))
            throw new EntradaInvalidaException(ValidadorEntrada.MensagemUsuarioSsh);

        var args = new List<string>
        {
            "-w", Constantes.JanelaTerminal, "nt",
            "--title", Titulo(demandaId, tituloDemanda, agente, pasta, rotulo), "--suppressApplicationTitle",
        };
        if (opcoes.UsarSsh) args.AddRange(["ssh", "-t", $"{opcoes.UsuarioSsh}@{Constantes.HostSsh}"]);
        args.AddRange(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Constantes.ScriptAbrir,
            "-Agente", Agentes.NoArquivo(agente)]);
        // Fora da raiz o caminho vai em Base64: a linha do ssh é reinterpretada e espaços ou "&" virariam outro comando.
        if (ValidadorEntrada.PastaValida(pasta)) args.AddRange(["-Repo", pasta]);
        else args.AddRange(["-Caminho", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pasta))]);
        if (sessionId is not null) { args.Add("-Sessao"); args.Add(sessionId); }
        args.Add("-Modo");
        args.Add(modo);
        return new ComandoTerminal(Constantes.ExecutavelTerminal, args);
    }
}
