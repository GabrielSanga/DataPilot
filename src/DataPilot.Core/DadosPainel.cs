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
                if (repo.TryGetPropertyValue("sessoes", out var ses) && ses is not null && ses is not JsonArray)
                    return "\"sessoes\" não é uma lista";
                if (repo.TryGetPropertyValue("lancamentos", out var lan) && lan is not null && lan is not JsonArray)
                    return "\"lancamentos\" não é uma lista";
            }
        }
        return null;
    }

    /// <summary>Extrai só o que passa na validação; o resto é desconsiderado e nunca chega a um comando.</summary>
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
                            if (!ValidadorEntrada.TentarAgente(Texto(s, "agente"), out var agente)) continue;
                            var sessionId = Texto(s, "sessionId");
                            if (!ValidadorEntrada.IdentificadorValido(sessionId) || !idsSessao.Add(sessionId!)) continue;
                            var rotulo = Texto(s, "rotulo");
                            if (!ValidadorEntrada.RotuloValido(rotulo)) rotulo = null;
                            var criada = DateTimeOffset.TryParse(Texto(s, "criadaEm"), CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out var c) ? c : DateTimeOffset.MinValue;
                            sessoes.Add(new SessaoRegistrada(agente, sessionId!, pasta!, rotulo, criada, id));
                        }
                    }
                    var lancamentos = new List<LancamentoPendente>();
                    if (repo["lancamentos"] is JsonArray pendentes)
                    {
                        foreach (var l in pendentes.OfType<JsonObject>())
                        {
                            if (!ValidadorEntrada.TentarAgente(Texto(l, "agente"), out var agenteLancamento)) continue;
                            var idLancamento = Texto(l, "id");
                            if (!ValidadorEntrada.IdentificadorValido(idLancamento)) continue;
                            if (!DateTimeOffset.TryParse(Texto(l, "criadoEm"), CultureInfo.InvariantCulture,
                                    DateTimeStyles.None, out var criadoEm)) continue;
                            var rotuloLancamento = Texto(l, "rotulo");
                            // Rótulo ausente vale; rótulo presente e inválido invalida o lançamento.
                            if (l.ContainsKey("rotulo") && l["rotulo"] is not null && !ValidadorEntrada.RotuloValido(rotuloLancamento)) continue;
                            if (!idsLancamento.Add(idLancamento!)) continue;
                            lancamentos.Add(new LancamentoPendente(idLancamento!, agenteLancamento, criadoEm, rotuloLancamento));
                        }
                    }
                    repositorios.Add(new RepositorioCadastrado(pasta!, sessoes, lancamentos));
                }
            }

            var openspec = Texto(demanda, "openspecChange");
            if (!ValidadorEntrada.OpenspecChangeValido(openspec)) openspec = null;
            DateTimeOffset? criadaEm = DateTimeOffset.TryParse(Texto(demanda, "criadaEm"), CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var cd) ? cd : null;
            var arquivada = demanda["arquivada"] is JsonValue a && a.TryGetValue<bool>(out var b) && b;

            resultado.Add(new Demanda(id, Texto(demanda, "titulo")!, openspec, criadaEm, arquivada, repositorios));
        }
        return resultado;
    }

    private static bool DemandaCarregavel(JsonObject demanda)
        => ValidadorEntrada.IdDemandaValido(Texto(demanda, "id")) && ValidadorEntrada.TituloValido(Texto(demanda, "titulo"));

    private static string? Texto(JsonObject o, string nome)
        => o[nome] is JsonValue v && v.TryGetValue<string>(out var t) ? t : null;

    // ---------- demandas ----------

    public ResultadoOperacao CriarDemanda(string id, string titulo, string? openspecChange, DateTimeOffset criadaEm)
    {
        var invalido = ValidarCampos(id, titulo, openspecChange);
        if (invalido is not null) return invalido;
        if (RegrasDemanda.EhImplicita(id))
            return ResultadoOperacao.Falha($"O identificador {IdDemandaImplicita} é reservado.");

        return Alterar(raiz =>
        {
            if (EncontrarDemanda(raiz, id) is not null)
                return ResultadoOperacao.Falha($"Já existe uma demanda com o identificador {id}.");

            var nova = new JsonObject { ["id"] = id, ["titulo"] = titulo };
            if (!string.IsNullOrEmpty(openspecChange)) nova["openspecChange"] = openspecChange;
            nova["criadaEm"] = criadaEm.ToString("o", CultureInfo.InvariantCulture);
            nova["arquivada"] = false;
            nova["repositorios"] = new JsonArray();
            raiz["demandas"]!.AsArray().Add(nova);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Altera <c>id</c>, <c>titulo</c> e <c>openspecChange</c>; repositórios e sessões ficam como estão.</summary>
    public ResultadoOperacao EditarDemanda(string idAtual, string novoId, string titulo, string? openspecChange)
    {
        if (RegrasDemanda.EhImplicita(idAtual))
            return ResultadoOperacao.Falha("A demanda \"Sem demanda\" não pode ser editada.");
        var invalido = ValidarCampos(novoId, titulo, openspecChange);
        if (invalido is not null) return invalido;
        if (RegrasDemanda.EhImplicita(novoId))
            return ResultadoOperacao.Falha($"O identificador {IdDemandaImplicita} é reservado.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, idAtual);
            if (demanda is null) return ResultadoOperacao.Falha($"A demanda {idAtual} não existe.");
            var outra = EncontrarDemanda(raiz, novoId);
            if (outra is not null && !ReferenceEquals(outra, demanda))
                return ResultadoOperacao.Falha($"Já existe uma demanda com o identificador {novoId}.");

            demanda["id"] = novoId;
            demanda["titulo"] = titulo;
            if (string.IsNullOrEmpty(openspecChange)) demanda.Remove("openspecChange");
            else demanda["openspecChange"] = openspecChange;
            return ResultadoOperacao.Sucesso;
        });
    }

    public ResultadoOperacao ArquivarDemanda(string id)
    {
        if (RegrasDemanda.EhImplicita(id))
            return ResultadoOperacao.Falha("A demanda \"Sem demanda\" não pode ser arquivada.");
        return DefinirArquivada(id, true);
    }

    public ResultadoOperacao DesarquivarDemanda(string id) => DefinirArquivada(id, false);

    private ResultadoOperacao DefinirArquivada(string id, bool arquivada)
        => Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, id);
            if (demanda is null) return ResultadoOperacao.Falha($"A demanda {id} não existe.");
            demanda["arquivada"] = arquivada;
            return ResultadoOperacao.Sucesso;
        });

    private static ResultadoOperacao? ValidarCampos(string id, string titulo, string? openspecChange)
    {
        if (!ValidadorEntrada.IdDemandaValido(id))
            return ResultadoOperacao.Falha("Identificador inválido: use de 1 a 30 caracteres entre letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.");
        if (!ValidadorEntrada.TituloValido(titulo))
            return ResultadoOperacao.Falha("Título inválido: use de 1 a 80 caracteres, sem caracteres de controle.");
        if (!string.IsNullOrEmpty(openspecChange) && !ValidadorEntrada.OpenspecChangeValido(openspecChange))
            return ResultadoOperacao.Falha("Change do OpenSpec inválida: use letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.");
        return null;
    }

    // ---------- repositórios ----------

    public ResultadoOperacao VincularRepositorio(string demandaId, string pasta)
    {
        if (!ValidadorEntrada.PastaValida(pasta))
            return ResultadoOperacao.Falha("Nome de pasta inválido: use apenas letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"A demanda {demandaId} não existe.");
            if (EncontrarRepositorio(demanda, pasta) is not null)
                return ResultadoOperacao.Falha($"O repositório {pasta} já está vinculado a esta demanda.");

            Repositorios(demanda).Add(NovoRepositorio(pasta));
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Remove o repositório e as sessões registradas nele, só nessa demanda. A confirmação é de quem chama.</summary>
    public ResultadoOperacao RemoverRepositorio(string demandaId, string pasta)
    {
        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"A demanda {demandaId} não existe.");
            if (demanda["repositorios"] is not JsonArray repos)
                return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a esta demanda.");

            var removido = false;
            for (var i = repos.Count - 1; i >= 0; i--)
            {
                if (repos[i] is JsonObject r && PastaIgual(r, pasta))
                {
                    repos.RemoveAt(i);
                    removido = true;
                }
            }
            return removido ? ResultadoOperacao.Sucesso
                : ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a esta demanda.");
        });
    }

    // ---------- sessões ----------

    public ResultadoOperacao RegistrarSessao(string demandaId, string pasta, Agente agente, string sessionId, string? rotulo, DateTimeOffset criadaEm)
        => Registrar(demandaId, pasta, agente, sessionId, rotulo, criadaEm, vinculadaEm: null, criarRepositorio: false);

    /// <summary>
    /// Vínculo explícito de uma sessão que o painel não abriu. Recebe só o <see cref="PedidoVinculo"/> (agente e
    /// identificador); demanda, pasta e rótulo vêm da escolha do usuário. Grava <c>vinculadaEm</c> (e usa a mesma
    /// data em <c>criadaEm</c>) e vincula o repositório à demanda quando faltar. Demanda arquivada não recebe vínculo.
    /// </summary>
    public ResultadoOperacao VincularSessao(PedidoVinculo pedido, string demandaId, string pasta, string? rotulo, DateTimeOffset vinculadaEm)
        => Registrar(demandaId, pasta, pedido.Agente, pedido.SessionId, rotulo, vinculadaEm, vinculadaEm, criarRepositorio: true);

    private ResultadoOperacao Registrar(string demandaId, string pasta, Agente agente, string sessionId, string? rotulo,
        DateTimeOffset criadaEm, DateTimeOffset? vinculadaEm, bool criarRepositorio)
    {
        if (!ValidadorEntrada.PastaValida(pasta)) return ResultadoOperacao.Falha("Repositório inválido.");
        if (!ValidadorEntrada.IdentificadorValido(sessionId)) return ResultadoOperacao.Falha("Identificador de sessão inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo)) return ResultadoOperacao.Falha("Rótulo inválido.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"A demanda {demandaId} não existe.");
            if (vinculadaEm is not null && demanda["arquivada"] is JsonValue arq && arq.TryGetValue<bool>(out var arquivada) && arquivada)
                return ResultadoOperacao.Falha($"A demanda {demandaId} está arquivada.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null && !criarRepositorio)
                return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a esta demanda.");
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
            ["agente"] = ValidadorEntrada.NomeAgente(agente),
            ["sessionId"] = sessionId,
        };
        if (rotulo is not null) sessao["rotulo"] = rotulo;
        sessao["criadaEm"] = criadaEm.ToString("o", CultureInfo.InvariantCulture);
        if (vinculadaEm is not null) sessao["vinculadaEm"] = vinculadaEm.Value.ToString("o", CultureInfo.InvariantCulture);
        sessoes.Add(sessao);
    }

    // ---------- lançamentos pendentes ----------

    /// <summary>
    /// Registra o lançamento pendente de uma sessão nova do Codex no repositório do card. O identificador do
    /// lançamento é gerado por quem chama. Lançamentos do repositório com mais de <see cref="ValidadeLancamento"/>
    /// são removidos na mesma gravação.
    /// </summary>
    public ResultadoOperacao RegistrarLancamento(string demandaId, string pasta, Agente agente, string lancamentoId,
        string? rotulo, DateTimeOffset criadoEm)
    {
        if (!ValidadorEntrada.PastaValida(pasta)) return ResultadoOperacao.Falha("Repositório inválido.");
        if (!ValidadorEntrada.IdentificadorValido(lancamentoId)) return ResultadoOperacao.Falha("Identificador de lançamento inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo)) return ResultadoOperacao.Falha("Rótulo inválido.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"A demanda {demandaId} não existe.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null) return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a esta demanda.");
            if (LancamentosComId(raiz, lancamentoId).Any()) return ResultadoOperacao.Falha("Este lançamento já está registrado.");

            var lista = repo["lancamentos"] as JsonArray;
            if (lista is null) repo["lancamentos"] = lista = new JsonArray();
            for (var i = lista.Count - 1; i >= 0; i--)
            {
                if (lista[i] is JsonObject l && LancamentoVencido(l, criadoEm)) lista.RemoveAt(i);
            }

            var novo = new JsonObject
            {
                ["id"] = lancamentoId,
                ["agente"] = ValidadorEntrada.NomeAgente(agente),
            };
            if (rotulo is not null) novo["rotulo"] = rotulo;
            novo["criadoEm"] = criadoEm.ToString("o", CultureInfo.InvariantCulture);
            lista.Add(novo);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>
    /// Cancela um lançamento pendente pela linha dele no card. Remove só esse lançamento e não pede confirmação; se
    /// ele não existir mais (usado ou removido por outra janela), nada é gravado.
    /// </summary>
    public ResultadoOperacao CancelarLancamento(string lancamentoId) => DesfazerLancamento(lancamentoId);

    /// <summary>
    /// Troca a data de um lançamento pendente, quando o usuário abre o agente de novo pela linha dele: a validade
    /// recomeça e só uma sessão criada depois disso corresponde a ele. Se o lançamento não existir mais, nada é gravado.
    /// </summary>
    public ResultadoOperacao RenovarLancamento(string lancamentoId, DateTimeOffset criadoEm)
    {
        return Alterar(raiz =>
        {
            var lancamento = LancamentosComId(raiz, lancamentoId).FirstOrDefault();
            if (lancamento is null) return ResultadoOperacao.Falha("O lançamento não existe mais.");
            lancamento["criadoEm"] = criadoEm.ToString("o", CultureInfo.InvariantCulture);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Desfaz o registro do lançamento quando o terminal não abre.</summary>
    public ResultadoOperacao DesfazerLancamento(string lancamentoId)
    {
        return Alterar(raiz =>
        {
            var removido = false;
            foreach (var repo in Todos(raiz))
            {
                if (repo["lancamentos"] is not JsonArray lista) continue;
                for (var i = lista.Count - 1; i >= 0; i--)
                {
                    if (lista[i] is JsonObject l && LancamentoIgual(l, lancamentoId))
                    {
                        lista.RemoveAt(i);
                        removido = true;
                    }
                }
            }
            return removido ? ResultadoOperacao.Sucesso : ResultadoOperacao.Falha("Lançamento não registrado.");
        });
    }

    /// <summary>
    /// Aplica, numa única gravação, o vínculo de uma sessão a um lançamento pendente: confere que o lançamento ainda
    /// existe (com o mesmo agente) e que o identificador não está registrado, registra a sessão com o rótulo do
    /// lançamento e <c>vinculadaEm</c>, e remove o lançamento. Demanda arquivada não recebe vínculo.
    /// </summary>
    public ResultadoOperacao VincularPorLancamento(PedidoVinculo pedido, string demandaId, string pasta,
        string lancamentoId, DateTimeOffset vinculadaEm)
    {
        if (!ValidadorEntrada.PastaValida(pasta)) return ResultadoOperacao.Falha("Repositório inválido.");
        if (!ValidadorEntrada.IdentificadorValido(pedido.SessionId)) return ResultadoOperacao.Falha("Identificador de sessão inválido.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"A demanda {demandaId} não existe.");
            if (demanda["arquivada"] is JsonValue arq && arq.TryGetValue<bool>(out var arquivada) && arquivada)
                return ResultadoOperacao.Falha($"A demanda {demandaId} está arquivada.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null) return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a esta demanda.");

            if (repo["lancamentos"] is not JsonArray lista
                || lista.OfType<JsonObject>().FirstOrDefault(l => LancamentoIgual(l, lancamentoId)) is not { } lancamento)
                return ResultadoOperacao.Falha("O lançamento não existe mais.");
            if (Texto(lancamento, "agente") != ValidadorEntrada.NomeAgente(pedido.Agente))
                return ResultadoOperacao.Falha("O agente da sessão não é o do lançamento.");
            if (SessoesComId(raiz, pedido.SessionId).Any())
                return ResultadoOperacao.Falha("Esta sessão já está registrada.");

            var rotulo = Texto(lancamento, "rotulo");
            if (!ValidadorEntrada.RotuloValido(rotulo)) rotulo = null;

            AdicionarSessao(repo, pedido.Agente, pedido.SessionId, rotulo, vinculadaEm, vinculadaEm);
            lista.Remove(lancamento);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Quanto tempo um lançamento não usado continua valendo.</summary>
    public static readonly TimeSpan ValidadeLancamento = TimeSpan.FromHours(24);

    private static bool LancamentoVencido(JsonObject lancamento, DateTimeOffset agora)
        => DateTimeOffset.TryParse(Texto(lancamento, "criadoEm"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var criado)
           && agora - criado > ValidadeLancamento;

    private static bool LancamentoIgual(JsonObject lancamento, string id)
        => string.Equals(Texto(lancamento, "id"), id, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<JsonObject> Todos(JsonObject raiz)
        => raiz["demandas"]!.AsArray().OfType<JsonObject>()
            .Select(d => d["repositorios"] as JsonArray).OfType<JsonArray>()
            .SelectMany(rs => rs.OfType<JsonObject>());

    private static IEnumerable<JsonObject> LancamentosComId(JsonObject raiz, string id)
        => Todos(raiz).Select(r => r["lancamentos"] as JsonArray).OfType<JsonArray>()
            .SelectMany(ls => ls.OfType<JsonObject>())
            .Where(l => LancamentoIgual(l, id));

    /// <summary>
    /// Move a sessão para a demanda de destino, no repositório de mesma pasta (criado ali se faltar).
    /// O nó da sessão é movido inteiro, então campos desconhecidos dela são preservados.
    /// </summary>
    public ResultadoOperacao MoverSessao(string sessionId, string demandaDestinoId)
    {
        return Alterar(raiz =>
        {
            var origem = Extrair(raiz).SelectMany(d => d.Repositorios.SelectMany(r => r.Sessoes))
                .FirstOrDefault(s => string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase));
            if (origem is null) return ResultadoOperacao.Falha("Sessão não registrada.");
            if (string.Equals(origem.DemandaId, demandaDestinoId, StringComparison.OrdinalIgnoreCase))
                return ResultadoOperacao.Falha("A sessão já está nessa demanda.");

            var destino = EncontrarDemanda(raiz, demandaDestinoId);
            if (destino is null) return ResultadoOperacao.Falha($"A demanda {demandaDestinoId} não existe.");

            var demandaOrigem = EncontrarDemanda(raiz, origem.DemandaId)!;
            var repoOrigem = EncontrarRepositorio(demandaOrigem, origem.Pasta)!;
            var lista = (JsonArray)repoOrigem["sessoes"]!;
            var indice = lista.ToList().FindIndex(n => n is JsonObject s && SessaoIgual(s, sessionId));
            var no = lista[indice]!;
            lista.RemoveAt(indice);

            var repoDestino = EncontrarRepositorio(destino, origem.Pasta);
            if (repoDestino is null)
            {
                repoDestino = NovoRepositorio(origem.Pasta);
                Repositorios(destino).Add(repoDestino);
            }
            if (repoDestino["sessoes"] is not JsonArray sessoes) repoDestino["sessoes"] = sessoes = new JsonArray();
            sessoes.Add(no);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Remove o registro de uma sessão. Nada do agente nem da pasta de status é tocado.</summary>
    public ResultadoOperacao RemoverSessao(string sessionId)
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
                        if (sessoes[i] is JsonObject s && SessaoIgual(s, sessionId))
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

    /// <summary>Desfaz o registro quando o terminal não abre.</summary>
    public ResultadoOperacao DesfazerRegistro(string sessionId) => RemoverSessao(sessionId);

    // ---------- apoio ----------

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

    /// <summary>A demanda que a carga considera para esse identificador (a primeira carregável).</summary>
    private static JsonObject? EncontrarDemanda(JsonObject raiz, string id)
        => raiz["demandas"]!.AsArray().OfType<JsonObject>()
            .FirstOrDefault(d => DemandaCarregavel(d) && string.Equals(Texto(d, "id"), id, StringComparison.OrdinalIgnoreCase));

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
