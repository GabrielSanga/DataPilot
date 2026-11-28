using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Persistence;

public sealed partial class DadosPainel
{
    public ResultadoOperacao RegistrarSessao(string demandaId, string pasta, Agente agente, string sessionId, string? rotulo, DateTimeOffset criadaEm)
        => Registrar(demandaId, pasta, agente, sessionId, rotulo, criadaEm, vinculadaEm: null, criarRepositorio: false);

    /// <summary>
    /// Vínculo explícito de uma sessão que o painel não abriu. Recebe só o <see cref="PedidoVinculo"/> (agente e
    /// identificador); demanda, pasta e rótulo vêm da escolha do usuário. Grava <c>vinculadaEm</c> (e usa a mesma
    /// data em <c>criadaEm</c>) e vincula o repositório à demanda quando faltar. Demanda arquivada não recebe vínculo.
    /// Uma sessão já registrada em "Sessões" (<c>sem-demanda</c>) é movida para a demanda escolhida, com o repositório
    /// e o rótulo escolhidos (sem rótulo escolhido, mantém o que tinha).
    /// </summary>
    public ResultadoOperacao VincularSessao(PedidoVinculo pedido, string demandaId, string pasta, string? rotulo, DateTimeOffset vinculadaEm)
    {
        if (!ValidadorEntrada.RepositorioValido(pasta)) return ResultadoOperacao.Falha(ValidadorEntrada.MensagemRepositorio);
        if (!ValidadorEntrada.IdentificadorValido(pedido.SessionId)) return ResultadoOperacao.Falha("Identificador de sessão inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo)) return ResultadoOperacao.Falha(ValidadorEntrada.MensagemRotulo);

        return Alterar(raiz =>
        {
            var demanda = EncontrarOuCriarImplicita(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (EstaArquivada(demanda))
                return ResultadoOperacao.Falha($"O trabalho {demandaId} está arquivado.");

            var existente = SessoesComId(raiz, pedido.SessionId).FirstOrDefault();
            JsonObject? no = null;
            if (existente is not null)
            {
                // Só a sessão registrada em "Sessões" pode ser movida; as de um trabalho continuam onde estão.
                var implicita = EncontrarDemanda(raiz, RegrasDemanda.IdDemandaImplicita);
                if (RegrasDemanda.EhImplicita(demandaId) || implicita is null || !ContemSessao(implicita, pedido.SessionId))
                    return ResultadoOperacao.Falha("Esta sessão já está registrada.");
                no = RetirarSessao(implicita, pedido.SessionId);
            }

            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null)
            {
                repo = NovoRepositorio(pasta);
                Repositorios(demanda).Add(repo);
            }

            if (no is null)
            {
                AdicionarSessao(repo, pedido.Agente, pedido.SessionId, rotulo, vinculadaEm, vinculadaEm);
                return ResultadoOperacao.Sucesso;
            }
            if (rotulo is not null) no["rotulo"] = rotulo;
            if (repo["sessoes"] is not JsonArray sessoes) repo["sessoes"] = sessoes = new JsonArray();
            sessoes.Add(no);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>
    /// "Retomar" numa sessão que o painel não registrou: registra-a em "Sessões" (<c>sem-demanda</c>), no repositório
    /// escolhido, com <c>vinculadaEm</c>. Só o <see cref="PedidoVinculo"/> (agente e GUID canônico) vem do arquivo de status.
    /// </summary>
    public ResultadoOperacao RegistrarRetomada(PedidoVinculo pedido, string pasta, DateTimeOffset vinculadaEm)
        => Registrar(RegrasDemanda.IdDemandaImplicita, pasta, pedido.Agente, pedido.SessionId, null, vinculadaEm, vinculadaEm,
            criarRepositorio: true);

    private static bool ContemSessao(JsonObject demanda, string sessionId)
        => demanda["repositorios"] is JsonArray repos
           && repos.OfType<JsonObject>().Any(r => r["sessoes"] is JsonArray ss && ss.OfType<JsonObject>().Any(s => SessaoIgual(s, sessionId)));

    /// <summary>Tira o nó da sessão da demanda e o devolve inteiro, para preservar campos desconhecidos.</summary>
    private static JsonObject? RetirarSessao(JsonObject demanda, string sessionId)
    {
        if (demanda["repositorios"] is not JsonArray repos) return null;
        foreach (var repo in repos.OfType<JsonObject>())
        {
            if (repo["sessoes"] is not JsonArray lista) continue;
            var indice = lista.ToList().FindIndex(n => n is JsonObject s && SessaoIgual(s, sessionId));
            if (indice < 0) continue;
            var no = (JsonObject)lista[indice]!;
            lista.RemoveAt(indice);
            return no;
        }
        return null;
    }

    private ResultadoOperacao Registrar(string demandaId, string pasta, Agente agente, string sessionId, string? rotulo,
        DateTimeOffset criadaEm, DateTimeOffset? vinculadaEm, bool criarRepositorio)
    {
        if (!ValidadorEntrada.RepositorioValido(pasta)) return ResultadoOperacao.Falha(ValidadorEntrada.MensagemRepositorio);
        if (!ValidadorEntrada.IdentificadorValido(sessionId)) return ResultadoOperacao.Falha("Identificador de sessão inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo)) return ResultadoOperacao.Falha(ValidadorEntrada.MensagemRotulo);

        return Alterar(raiz =>
        {
            var demanda = EncontrarOuCriarImplicita(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (vinculadaEm is not null && EstaArquivada(demanda))
                return ResultadoOperacao.Falha($"O trabalho {demandaId} está arquivado.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null && !criarRepositorio)
                return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");
            if (SessoesComId(raiz, sessionId).Any())
                return ResultadoOperacao.Falha("Esta sessão já está registrada.");
            if (repo is null)
            {
                repo = NovoRepositorio(pasta);
                Repositorios(demanda).Add(repo);
            }

            AdicionarSessao(repo, agente, sessionId, rotulo, criadaEm, vinculadaEm);
            return ResultadoOperacao.Sucesso;
        });
    }

    private static void AdicionarSessao(JsonObject repo, Agente agente, string sessionId, string? rotulo,
        DateTimeOffset criadaEm, DateTimeOffset? vinculadaEm)
    {
        var sessoes = repo["sessoes"] as JsonArray;
        if (sessoes is null) repo["sessoes"] = sessoes = new JsonArray();

        var sessao = new JsonObject
        {
            ["agente"] = Agentes.NoArquivo(agente),
            ["sessionId"] = sessionId,
            ["execucao"] = ExecucaoContainer,
        };
        if (rotulo is not null) sessao["rotulo"] = rotulo;
        sessao["criadaEm"] = criadaEm.ToString("o", CultureInfo.InvariantCulture);
        if (vinculadaEm is not null) sessao["vinculadaEm"] = vinculadaEm.Value.ToString("o", CultureInfo.InvariantCulture);
        sessoes.Add(sessao);
    }


    /// <summary>
    /// Exclui a sessão do painel numa única gravação: remove o registro, se houver, e acrescenta o agente, o
    /// identificador e a data à lista <c>excluidas</c>. Nada do agente nem da pasta de status é tocado. Excluir de novo
    /// uma sessão já excluída não duplica o item.
    /// </summary>
    public ResultadoOperacao ExcluirSessao(Agente agente, string sessionId, DateTimeOffset excluidaEm)
    {
        if (!Enum.IsDefined(agente)) return ResultadoOperacao.Falha("Agente inválido.");
        if (!ValidadorEntrada.IdentificadorExclusaoValido(sessionId)) return ResultadoOperacao.Falha("Identificador de sessão inválido.");

        return Alterar(raiz =>
        {
            RetirarSessoes(raiz, sessionId);

            if (raiz["excluidas"] is not JsonArray lista) raiz["excluidas"] = lista = new JsonArray();
            var jaExcluida = lista.OfType<JsonObject>().Any(e =>
                string.Equals(Texto(e, "sessionId"), sessionId, StringComparison.OrdinalIgnoreCase)
                && Texto(e, "agente") == Agentes.NoArquivo(agente));
            if (!jaExcluida)
                lista.Add(new JsonObject
                {
                    ["agente"] = Agentes.NoArquivo(agente),
                    ["sessionId"] = sessionId,
                    ["excluidaEm"] = excluidaEm.ToString("o", CultureInfo.InvariantCulture),
                });
            return ResultadoOperacao.Sucesso;
        });
    }

    private static List<SessaoExcluida> ExtrairExcluidas(JsonObject raiz)
    {
        var resultado = new List<SessaoExcluida>();
        if (raiz["excluidas"] is not JsonArray lista) return resultado;
        foreach (var e in lista.OfType<JsonObject>())
        {
            if (!Agentes.TentarLer(Texto(e, "agente"), out var agente)) continue;
            var id = Texto(e, "sessionId");
            if (!ValidadorEntrada.IdentificadorExclusaoValido(id)) continue;
            var data = DateTimeOffset.TryParse(Texto(e, "excluidaEm"), CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d) ? d : DateTimeOffset.MinValue;
            resultado.Add(new SessaoExcluida(agente, id!, data));
        }
        return resultado;
    }

    /// <summary>Remove o registro de uma sessão (também usado para desfazer o registro quando o terminal não abre). Nada do agente nem da pasta de status é tocado.</summary>
    public ResultadoOperacao DesfazerRegistro(string sessionId)
        => Alterar(raiz => RetirarSessoes(raiz, sessionId) ? ResultadoOperacao.Sucesso : ResultadoOperacao.Falha("Sessão não registrada."));
}
