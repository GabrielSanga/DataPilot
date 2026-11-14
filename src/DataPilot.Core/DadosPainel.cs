using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DataPilot.Core;

/// <summary>
/// Arquivo de dados do painel (<c>demandas.json</c>). Lê como árvore JSON, altera só os nós que conhece
/// e grava a árvore inteira, de modo que campos desconhecidos são preservados. Cada operação relê o arquivo
/// e grava por temporário e troca. Arquivo ilegível nunca é sobrescrito.
/// </summary>
public sealed class DadosPainel(string caminho)
{
    public const string IdDemandaImplicita = "sem-demanda";
    public const string TituloDemandaImplicita = "Sem demanda";

    private static readonly JsonSerializerOptions Gravacao = new()
    {
        WriteIndented = true,
        // Mantém acentos legíveis para quem editar o arquivo à mão.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Caminho { get; } = caminho;

    public static string CaminhoPadrao => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Painel", "demandas.json");

    // ---------- leitura ----------

    public EstadoDados Carregar()
    {
        var leitura = Ler();
        return leitura.Erro is not null
            ? new EstadoDados(false, [], leitura.Erro)
            : new EstadoDados(true, Extrair(leitura.Raiz), null);
    }

    private readonly record struct Leitura(JsonObject Raiz, string? Erro);

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

    private static Leitura Ilegivel(string motivo) => new(NovaRaiz(), motivo);

    private static JsonObject NovaRaiz() => new() { ["demandas"] = new JsonArray() };

    /// <summary>Confere só os contêineres: o que não for do tipo esperado torna o arquivo ilegível.</summary>
    private static string? VerificarEstrutura(JsonObject raiz)
    {
        if (!raiz.TryGetPropertyValue("demandas", out var demandas) || demandas is null) return null;
        if (demandas is not JsonArray lista) return "\"demandas\" não é uma lista";
        foreach (var d in lista)
        {
            if (d is not JsonObject demanda) return "uma demanda não é um objeto";
            if (!demanda.TryGetPropertyValue("repositorios", out var repos) || repos is null) continue;
            if (repos is not JsonArray repositorios) return "\"repositorios\" não é uma lista";
            foreach (var r in repositorios)
            {
                if (r is not JsonObject repo) continue; // entrada desconsiderada na carga
                if (!repo.TryGetPropertyValue("sessoes", out var ses) || ses is null) continue;
                if (ses is not JsonArray) return "\"sessoes\" não é uma lista";
            }
        }
        return null;
    }

    /// <summary>Extrai só o que passa na validação; o resto é desconsiderado e nunca chega a um comando.</summary>
    private static List<RepositorioCadastrado> Extrair(JsonObject raiz)
    {
        var resultado = new List<RepositorioCadastrado>();
        var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (raiz["demandas"] is not JsonArray demandas) return resultado;

        foreach (var demanda in demandas.OfType<JsonObject>())
        {
            if (demanda["repositorios"] is not JsonArray repos) continue;
            foreach (var repo in repos.OfType<JsonObject>())
            {
                var pasta = Texto(repo, "pasta");
                if (!ValidadorEntrada.PastaValida(pasta) || !vistas.Add(pasta!)) continue;

                var sessoes = new List<SessaoRegistrada>();
                if (repo["sessoes"] is JsonArray lista)
                {
                    foreach (var s in lista.OfType<JsonObject>())
                    {
                        if (!ValidadorEntrada.TentarAgente(Texto(s, "agente"), out var agente)) continue;
                        var id = Texto(s, "sessionId");
                        if (!ValidadorEntrada.IdentificadorValido(id)) continue;
                        var rotulo = Texto(s, "rotulo");
                        if (!ValidadorEntrada.RotuloValido(rotulo)) rotulo = null;
                        var criada = DateTimeOffset.TryParse(Texto(s, "criadaEm"), CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var c) ? c : DateTimeOffset.MinValue;
                        sessoes.Add(new SessaoRegistrada(agente, id!, pasta!, rotulo, criada));
                    }
                }
                resultado.Add(new RepositorioCadastrado(pasta!, sessoes));
            }
        }
        return resultado;
    }

    private static string? Texto(JsonObject o, string nome)
        => o[nome] is JsonValue v && v.TryGetValue<string>(out var t) ? t : null;

    // ---------- operações ----------

    public ResultadoOperacao Cadastrar(string pasta)
    {
        if (!ValidadorEntrada.PastaValida(pasta))
            return ResultadoOperacao.Falha("Nome de pasta inválido: use apenas letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.");

        return Alterar(raiz =>
        {
            if (Extrair(raiz).Any(r => string.Equals(r.Pasta, pasta, StringComparison.OrdinalIgnoreCase)))
                return ResultadoOperacao.Falha($"O repositório {pasta} já está cadastrado.");

            DemandaImplicita(raiz)["repositorios"]!.AsArray().Add(new JsonObject
            {
                ["nome"] = pasta,
                ["pasta"] = pasta,
                ["sessoes"] = new JsonArray(),
            });
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Remove o repositório e as sessões registradas nele. A confirmação é de quem chama.</summary>
    public ResultadoOperacao Remover(string pasta)
    {
        return Alterar(raiz =>
        {
            var removido = false;
            foreach (var demanda in raiz["demandas"]!.AsArray().OfType<JsonObject>())
            {
                if (demanda["repositorios"] is not JsonArray repos) continue;
                for (var i = repos.Count - 1; i >= 0; i--)
                {
                    if (repos[i] is JsonObject r && ValidadorEntrada.PastaValida(Texto(r, "pasta"))
                        && string.Equals(Texto(r, "pasta"), pasta, StringComparison.OrdinalIgnoreCase))
                    {
                        repos.RemoveAt(i);
                        removido = true;
                    }
                }
            }
            return removido ? ResultadoOperacao.Sucesso : ResultadoOperacao.Falha($"O repositório {pasta} não está cadastrado.");
        });
    }

    public ResultadoOperacao RegistrarSessao(string pasta, Agente agente, string sessionId, string? rotulo, DateTimeOffset criadaEm)
    {
        if (!ValidadorEntrada.PastaValida(pasta)) return ResultadoOperacao.Falha("Repositório inválido.");
        if (!ValidadorEntrada.IdentificadorValido(sessionId)) return ResultadoOperacao.Falha("Identificador de sessão inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo)) return ResultadoOperacao.Falha("Rótulo inválido.");

        return Alterar(raiz =>
        {
            var repo = EncontrarRepositorio(raiz, pasta);
            if (repo is null) return ResultadoOperacao.Falha($"O repositório {pasta} não está cadastrado.");

            var sessoes = repo["sessoes"] as JsonArray;
            if (sessoes is null) repo["sessoes"] = sessoes = new JsonArray();

            var sessao = new JsonObject
            {
                ["agente"] = ValidadorEntrada.NomeAgente(agente),
                ["sessionId"] = sessionId,
            };
            if (rotulo is not null) sessao["rotulo"] = rotulo;
            sessao["criadaEm"] = criadaEm.ToString("o", CultureInfo.InvariantCulture);
            sessoes.Add(sessao);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Remove o registro de uma sessão (desfazer quando o terminal não abre).</summary>
    public ResultadoOperacao DesfazerRegistro(string sessionId)
    {
        return Alterar(raiz =>
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
                        if (sessoes[i] is JsonObject s
                            && string.Equals(Texto(s, "sessionId"), sessionId, StringComparison.OrdinalIgnoreCase))
                        {
                            sessoes.RemoveAt(i);
                            removido = true;
                        }
                    }
                }
            }
            return removido ? ResultadoOperacao.Sucesso : ResultadoOperacao.Falha("Sessão não registrada.");
        });
    }

    // ---------- apoio ----------

    private static JsonObject? EncontrarRepositorio(JsonObject raiz, string pasta)
    {
        foreach (var demanda in raiz["demandas"]!.AsArray().OfType<JsonObject>())
        {
            if (demanda["repositorios"] is not JsonArray repos) continue;
            foreach (var repo in repos.OfType<JsonObject>())
            {
                var p = Texto(repo, "pasta");
                if (ValidadorEntrada.PastaValida(p) && string.Equals(p, pasta, StringComparison.OrdinalIgnoreCase))
                    return repo;
            }
        }
        return null;
    }

    private static JsonObject DemandaImplicita(JsonObject raiz)
    {
        if (raiz["demandas"] is not JsonArray demandas) raiz["demandas"] = demandas = new JsonArray();

        var existente = demandas.OfType<JsonObject>().FirstOrDefault(d => Texto(d, "id") == IdDemandaImplicita);
        if (existente is null)
        {
            existente = new JsonObject
            {
                ["id"] = IdDemandaImplicita,
                ["titulo"] = TituloDemandaImplicita,
                ["arquivada"] = false,
                ["repositorios"] = new JsonArray(),
            };
            demandas.Add(existente);
        }
        if (existente["repositorios"] is not JsonArray) existente["repositorios"] = new JsonArray();
        return existente;
    }

    /// <summary>Relê, aplica a mudança e grava. Arquivo ilegível: nada é gravado.</summary>
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

    /// <summary>Grava num temporário na mesma pasta e troca sobre o destino: o arquivo tem sempre a versão anterior ou a nova.</summary>
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
}
