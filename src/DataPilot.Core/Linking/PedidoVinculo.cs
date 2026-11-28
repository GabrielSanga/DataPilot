using DataPilot.Core.Display;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Linking;

/// <summary>
/// Pedido de vínculo de uma sessão que o painel não abriu. É o único ponto em que um valor lido de arquivo
/// de status passa ao arquivo de dados: só o agente e o identificador (GUID canônico) cruzam. Não há como
/// criar um pedido sem passar por <see cref="Criar"/>, nem como carregar nele a pasta, o status ou a data.
/// </summary>
public sealed class PedidoVinculo
{
    public Agente Agente { get; }
    public string SessionId { get; }

    private PedidoVinculo(Agente agente, string sessionId)
    {
        Agente = agente;
        SessionId = sessionId;
    }

    /// <summary>Cria o pedido a partir de uma sessão lida; null se o identificador não for um GUID canônico.</summary>
    public static PedidoVinculo? Criar(Sessao lida)
        => ValidadorEntrada.IdentificadorValido(lida.SessionId) ? new PedidoVinculo(lida.Agente, lida.SessionId) : null;

    /// <summary>
    /// Cria o pedido a partir de uma sessão registrada, cujo agente e identificador vêm do arquivo de dados; null se
    /// o identificador não for um GUID canônico.
    /// </summary>
    public static PedidoVinculo? Criar(SessaoRegistrada registrada)
        => ValidadorEntrada.IdentificadorValido(registrada.SessionId) ? new PedidoVinculo(registrada.Agente, registrada.SessionId) : null;
}

/// <summary>Regras de tela do vínculo, como funções puras.</summary>
public static class RegrasVinculo
{
    public const string MotivoIdentificadorInvalido =
        "O identificador desta sessão não é um GUID; o painel não conseguiria retomá-la.";

    /// <summary>
    /// Repositório da raiz: o último segmento da pasta de trabalho é o nome dele. Repositório com caminho completo: a
    /// pasta de trabalho é o caminho dele no container. Sem diferenciar maiúsculas. Só decide pré-seleção, aviso e vínculo
    /// automático; nunca é gravada.
    /// </summary>
    public static bool PastaCoincide(string pastaDeTrabalho, string repositorio)
    {
        if (!ValidadorEntrada.CaminhoValido(repositorio))
            return string.Equals(TextoExibicao.NomePasta(pastaDeTrabalho), repositorio, StringComparison.OrdinalIgnoreCase);
        return string.Equals(pastaDeTrabalho.TrimEnd('/'), LocaisContainer.Traduzir(repositorio), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Avisar antes de vincular quando a pasta informada pela sessão não coincide com o repositório escolhido.</summary>
    public static bool AvisarPastaDivergente(string pastaDeTrabalho, string repositorio) => !PastaCoincide(pastaDeTrabalho, repositorio);
}
