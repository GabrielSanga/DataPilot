using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Validation;

namespace DataPilot.Core.Persistence;

/// <summary>
/// Arquivo de dados do painel (<c>demandas.json</c>). Lê como árvore JSON, altera só os nós que conhece
/// e grava a árvore inteira, de modo que campos desconhecidos são preservados. Cada operação relê o arquivo
/// e grava por temporário e troca. Arquivo ilegível nunca é sobrescrito.
/// </summary>
public sealed partial class DadosPainel(string caminho)
{
    private static readonly JsonSerializerOptions Gravacao = new()
    {
        WriteIndented = true,
        // Mantém acentos legíveis para quem editar o arquivo à mão.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Caminho { get; } = caminho;

    public static string CaminhoPadrao => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Painel", "demandas.json");

    // ---------- leitura ----------
    private readonly record struct Leitura(JsonObject Raiz, string? Erro);

    private ResultadoOperacao Alterar(Func<JsonObject, ResultadoOperacao> mudanca)
    {
        var leitura = Ler();
        if (leitura.Erro is not null)
            return ResultadoOperacao.Falha($"Arquivo de dados ilegível ({leitura.Erro}): {Caminho}");

        var raiz = leitura.Raiz;
        if (raiz["demandas"] is not JsonArray) raiz["demandas"] = new JsonArray();

        var resultado = mudanca(raiz);
        if (!resultado.Ok) return resultado;

        try
        {
            Gravar(raiz);
            return ResultadoOperacao.Sucesso;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return ResultadoOperacao.Falha($"Não foi possível gravar {Caminho}: {e.Message}");
        }
    }
    private void Gravar(JsonObject raiz)
    {
        var pasta = Path.GetDirectoryName(Path.GetFullPath(Caminho))!;
        Directory.CreateDirectory(pasta);
        var temporario = Path.Combine(pasta, Path.GetFileName(Caminho) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporario, raiz.ToJsonString(Gravacao), new UTF8Encoding(false));
            if (File.Exists(Caminho)) File.Replace(temporario, Caminho, null);
            else File.Move(temporario, Caminho);
        }
        finally
        {
            try { if (File.Exists(temporario)) File.Delete(temporario); } catch { }
        }
    }

    private Leitura Ler()
    {
        if (!File.Exists(Caminho)) return new Leitura(NovaRaiz(), null);
        try
        {
            var texto = File.ReadAllText(Caminho, Encoding.UTF8);
            var no = JsonNode.Parse(texto);
            if (no is not JsonObject raiz) return Ilegivel("a raiz do arquivo não é um objeto JSON");
            var problema = VerificarEstrutura(raiz);
            return problema is null ? new Leitura(raiz, null) : Ilegivel(problema);
        }
        catch (Exception e) when (e is JsonException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            return Ilegivel(e is JsonException or ArgumentException ? "o conteúdo não é JSON válido" : e.Message);
        }
    }

    public EstadoDados Carregar()
    {
        var leitura = Ler();
        return leitura.Erro is not null
            ? new EstadoDados(false, [], leitura.Erro)
            : new EstadoDados(true, Extrair(leitura.Raiz), null) { Excluidas = ExtrairExcluidas(leitura.Raiz) };
    }

    private static Leitura Ilegivel(string motivo) => new(NovaRaiz(), motivo);

    private static JsonObject NovaRaiz() => new() { ["demandas"] = new JsonArray() };

    private static string? VerificarEstrutura(JsonObject raiz)
    {
        if (raiz.TryGetPropertyValue("excluidas", out var excl) && excl is not null && excl is not JsonArray)
            return "\"excluidas\" não é uma lista";
        if (!raiz.TryGetPropertyValue("demandas", out var demandas) || demandas is null) return null;
        if (demandas is not JsonArray lista) return "\"demandas\" não é uma lista";
        foreach (var d in lista)
        {
            if (d is not JsonObject demanda) return "um item de \"demandas\" não é um objeto";
            if (!demanda.TryGetPropertyValue("repositorios", out var repos) || repos is null) continue;
            if (repos is not JsonArray repositorios) return "\"repositorios\" não é uma lista";
            foreach (var r in repositorios)
            {
                if (r is not JsonObject repo) continue; // entrada desconsiderada na carga
                if (repo.TryGetPropertyValue("sessoes", out var ses) && ses is not null && ses is not JsonArray)
                    return "\"sessoes\" não é uma lista";
                if (repo.TryGetPropertyValue("lancamentos", out var lan) && lan is not null && lan is not JsonArray)
                    return "\"lancamentos\" não é uma lista";
            }
        }
        return null;
    }

    private static List<Demanda> Extrair(JsonObject raiz)
    {
        var resultado = new List<Demanda>();
        var idsDemanda = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var idsSessao = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var idsLancamento = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (raiz["demandas"] is not JsonArray demandas) return resultado;

        foreach (var demanda in demandas.OfType<JsonObject>())
        {
            if (!DemandaCarregavel(demanda)) continue;
            var id = Texto(demanda, "id")!;
            if (!idsDemanda.Add(id)) continue;

            var tituloDemanda = Texto(demanda, "titulo")!;
            var openspecDemanda = Texto(demanda, "openspecChange");
            if (!ValidadorEntrada.OpenspecChangeValido(openspecDemanda)) openspecDemanda = null;

            var repositorios = new List<RepositorioCadastrado>();
            var pastas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (demanda["repositorios"] is JsonArray repos)
            {
                foreach (var repo in repos.OfType<JsonObject>())
                {
                    var pasta = Texto(repo, "pasta");
                    if (!ValidadorEntrada.PastaValida(pasta) || !pastas.Add(pasta!)) continue;

                    var sessoes = new List<SessaoRegistrada>();
                    if (repo["sessoes"] is JsonArray lista)
                    {
                        foreach (var s in lista.OfType<JsonObject>())
                        {
                            if (!Agentes.TentarLer(Texto(s, "agente"), out var agente)) continue;
                            var sessionId = Texto(s, "sessionId");
                            if (!ValidadorEntrada.IdentificadorValido(sessionId) || !idsSessao.Add(sessionId!)) continue;
                            var rotulo = Texto(s, "rotulo");
                            if (!ValidadorEntrada.RotuloValido(rotulo)) rotulo = null;
                            var criada = DateTimeOffset.TryParse(Texto(s, "criadaEm"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var c) ? c : DateTimeOffset.MinValue;
                            sessoes.Add(new SessaoRegistrada(agente, sessionId!, pasta!, rotulo, criada, id, tituloDemanda));
                        }
                    }
                    var lancamentos = new List<LancamentoPendente>();
                    if (repo["lancamentos"] is JsonArray pendentes)
                    {
                        foreach (var l in pendentes.OfType<JsonObject>())
                        {
                            if (!Agentes.TentarLer(Texto(l, "agente"), out var agenteLancamento)) continue;
                            var idLancamento = Texto(l, "id");
                            if (!ValidadorEntrada.IdentificadorValido(idLancamento)) continue;
                            if (!DateTimeOffset.TryParse(Texto(l, "criadoEm"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var criadoEm)) continue;
                            var rotuloLancamento = Texto(l, "rotulo");
                            // Rótulo ausente vale; rótulo presente e inválido invalida o lançamento.
                            if (l.ContainsKey("rotulo") && l["rotulo"] is not null && !ValidadorEntrada.RotuloValido(rotuloLancamento)) continue;
                            if (!idsLancamento.Add(idLancamento!)) continue;
                            lancamentos.Add(new LancamentoPendente(idLancamento!, agenteLancamento, criadoEm, rotuloLancamento));
                        }
                    }
                    var changeRepo = Texto(repo, "openspecChange");
                    if (changeRepo == "") changeRepo = null; // "Nenhuma" escolhida pelo usuário: não herda da demanda
                    else if (!ValidadorEntrada.OpenspecChangeValido(changeRepo)) changeRepo = openspecDemanda;
                    repositorios.Add(new RepositorioCadastrado(pasta!, sessoes, lancamentos, changeRepo));
                }
            }

            DateTimeOffset? criadaEm = DateTimeOffset.TryParse(Texto(demanda, "criadaEm"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var cd) ? cd : null;
            var arquivada = EstaArquivada(demanda);

            resultado.Add(new Demanda(id, tituloDemanda, openspecDemanda, criadaEm, arquivada, repositorios));
        }
        return resultado;
    }

    private static bool DemandaCarregavel(JsonObject demanda)
        => ValidadorEntrada.IdDemandaValido(Texto(demanda, "id")) && ValidadorEntrada.TituloValido(Texto(demanda, "titulo"));

    private static string? Texto(JsonObject o, string nome)
        => o[nome] is JsonValue v && v.TryGetValue<string>(out var t) ? t : null;

    private static bool EstaArquivada(JsonObject demanda)
        => demanda["arquivada"] is JsonValue v && v.TryGetValue<bool>(out var arquivada) && arquivada;

    private static bool RetirarSessoes(JsonObject raiz, string sessionId)
    {
        var removido = false;
        foreach (var demanda in raiz["demandas"]!.AsArray().OfType<JsonObject>())
        {
            if (demanda["repositorios"] is not JsonArray repos) continue;
            foreach (var repo in repos.OfType<JsonObject>())
            {
                if (repo["sessoes"] is not JsonArray sessoes) continue;
                for (var i = sessoes.Count - 1; i >= 0; i--)
                {
                    if (sessoes[i] is JsonObject s && SessaoIgual(s, sessionId))
                    {
                        sessoes.RemoveAt(i);
                        removido = true;
                    }
                }
            }
        }
        return removido;
    }

    private static bool SessaoIgual(JsonObject sessao, string sessionId)
        => string.Equals(Texto(sessao, "sessionId"), sessionId, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<JsonObject> SessoesComId(JsonObject raiz, string sessionId)
        => raiz["demandas"]!.AsArray().OfType<JsonObject>()
            .Select(d => d["repositorios"] as JsonArray).OfType<JsonArray>()
            .SelectMany(rs => rs.OfType<JsonObject>())
            .Select(r => r["sessoes"] as JsonArray).OfType<JsonArray>()
            .SelectMany(ss => ss.OfType<JsonObject>())
            .Where(s => SessaoIgual(s, sessionId));

    private static bool PastaIgual(JsonObject repo, string pasta)
    {
        var p = Texto(repo, "pasta");
        return ValidadorEntrada.PastaValida(p) && string.Equals(p, pasta, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonObject? EncontrarDemanda(JsonObject raiz, string id)
        => raiz["demandas"]!.AsArray().OfType<JsonObject>()
            .FirstOrDefault(d => DemandaCarregavel(d) && string.Equals(Texto(d, "id"), id, StringComparison.OrdinalIgnoreCase));

    private static JsonObject? EncontrarOuCriarImplicita(JsonObject raiz, string id)
    {
        var existente = EncontrarDemanda(raiz, id);
        if (existente is not null || !RegrasDemanda.EhImplicita(id)) return existente;
        var nova = new JsonObject
        {
            ["id"] = RegrasDemanda.IdDemandaImplicita,
            ["titulo"] = RegrasDemanda.TituloDemandaImplicita,
            ["arquivada"] = false,
            ["repositorios"] = new JsonArray(),
        };
        raiz["demandas"]!.AsArray().Add(nova);
        return nova;
    }

    private static JsonObject? EncontrarRepositorio(JsonObject demanda, string pasta)
        => demanda["repositorios"] is JsonArray repos
            ? repos.OfType<JsonObject>().FirstOrDefault(r => PastaIgual(r, pasta))
            : null;

    private static JsonArray Repositorios(JsonObject demanda)
    {
        if (demanda["repositorios"] is not JsonArray repos) demanda["repositorios"] = repos = new JsonArray();
        return repos;
    }

    private static JsonObject NovoRepositorio(string pasta) => new()
    {
        ["nome"] = pasta,
        ["pasta"] = pasta,
        ["sessoes"] = new JsonArray(),
    };

}
