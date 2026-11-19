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
            : new EstadoDados(true, Extrair(leitura.Raiz), null) { Excluidas = ExtrairExcluidas(leitura.Raiz) };
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
                            if (!ValidadorEntrada.TentarAgente(Texto(s, "agente"), out var agente)) continue;
                            var sessionId = Texto(s, "sessionId");
                            if (!ValidadorEntrada.IdentificadorValido(sessionId) || !idsSessao.Add(sessionId!)) continue;
                            var rotulo = Texto(s, "rotulo");
                            if (!ValidadorEntrada.RotuloValido(rotulo)) rotulo = null;
                            var criada = DateTimeOffset.TryParse(Texto(s, "criadaEm"), CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out var c) ? c : DateTimeOffset.MinValue;
                            sessoes.Add(new SessaoRegistrada(agente, sessionId!, pasta!, rotulo, criada, id, tituloDemanda));
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
                    var changeRepo = Texto(repo, "openspecChange");
                    if (changeRepo == "") changeRepo = null; // "Nenhuma" escolhida pelo usuário: não herda da demanda
                    else if (!ValidadorEntrada.OpenspecChangeValido(changeRepo)) changeRepo = openspecDemanda;
                    repositorios.Add(new RepositorioCadastrado(pasta!, sessoes, lancamentos, changeRepo));
                }
            }

            DateTimeOffset? criadaEm = DateTimeOffset.TryParse(Texto(demanda, "criadaEm"), CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var cd) ? cd : null;
            var arquivada = demanda["arquivada"] is JsonValue a && a.TryGetValue<bool>(out var b) && b;

            resultado.Add(new Demanda(id, tituloDemanda, openspecDemanda, criadaEm, arquivada, repositorios));
        }
        return resultado;
    }

    private static bool DemandaCarregavel(JsonObject demanda)
        => ValidadorEntrada.IdDemandaValido(Texto(demanda, "id")) && ValidadorEntrada.TituloValido(Texto(demanda, "titulo"));

    private static string? Texto(JsonObject o, string nome)
        => o[nome] is JsonValue v && v.TryGetValue<string>(out var t) ? t : null;

    // ---------- demandas ----------

    /// <summary>
    /// Cria um trabalho só com o título. O identificador (<c>t</c> e 8 dígitos hexadecimais de um GUID novo) é gerado
    /// aqui, conferido contra os existentes, e devolvido em <see cref="ResultadoOperacao.Id"/>; nunca é pedido nem exibido.
    /// </summary>
    public ResultadoOperacao CriarDemanda(string titulo, DateTimeOffset criadaEm, Func<Guid>? gerarGuid = null)
        => Criar(titulo, criadaEm, gerarGuid ?? Guid.NewGuid, idFixo: null);

    /// <summary>Criação com identificador escolhido, só para montar cenários de teste (dados gravados por versões anteriores).</summary>
    internal ResultadoOperacao CriarDemandaComId(string id, string titulo, DateTimeOffset criadaEm)
    {
        if (!ValidadorEntrada.IdDemandaValido(id))
            return ResultadoOperacao.Falha("Identificador inválido.");
        if (RegrasDemanda.EhImplicita(id))
            return ResultadoOperacao.Falha($"O identificador {IdDemandaImplicita} é reservado.");
        return Criar(titulo, criadaEm, Guid.NewGuid, idFixo: id);
    }

    private ResultadoOperacao Criar(string titulo, DateTimeOffset criadaEm, Func<Guid> guid, string? idFixo)
    {
        var invalido = ValidarTitulo(titulo);
        if (invalido is not null) return invalido;

        string? criado = null;
        var r = Alterar(raiz =>
        {
            var existentes = raiz["demandas"]!.AsArray().OfType<JsonObject>()
                .Select(d => Texto(d, "id")).OfType<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            existentes.Add(IdDemandaImplicita);

            string? id = null;
            if (idFixo is not null)
            {
                if (existentes.Contains(idFixo))
                    return ResultadoOperacao.Falha($"Já existe um trabalho com o identificador {idFixo}.");
                id = idFixo;
            }
            for (var tentativa = 0; tentativa < 100 && id is null; tentativa++)
            {
                var candidato = "t" + guid().ToString("N")[..8];
                if (!existentes.Contains(candidato)) id = candidato;
            }
            if (id is null) return ResultadoOperacao.Falha("Não foi possível gerar um identificador para o trabalho.");

            var nova = new JsonObject { ["id"] = id, ["titulo"] = titulo };
            nova["criadaEm"] = criadaEm.ToString("o", CultureInfo.InvariantCulture);
            nova["arquivada"] = false;
            nova["repositorios"] = new JsonArray();
            raiz["demandas"]!.AsArray().Add(nova);
            criado = id;
            return ResultadoOperacao.Sucesso;
        });
        return r.Ok ? r with { Id = criado } : r;
    }


    /// <summary>Altera só o <c>titulo</c>; <c>id</c>, repositórios e sessões ficam como estão.</summary>
    public ResultadoOperacao EditarDemanda(string id, string titulo)
    {
        if (RegrasDemanda.EhImplicita(id))
            return ResultadoOperacao.Falha("\"Sessões\" não pode ser editada.");
        var invalido = ValidarTitulo(titulo);
        if (invalido is not null) return invalido;

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, id);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {id} não existe.");
            demanda["titulo"] = titulo;
            return ResultadoOperacao.Sucesso;
        });
    }

    public ResultadoOperacao ArquivarDemanda(string id)
    {
        if (RegrasDemanda.EhImplicita(id))
            return ResultadoOperacao.Falha("\"Sessões\" não pode ser arquivada.");
        return DefinirArquivada(id, true);
    }

    public ResultadoOperacao DesarquivarDemanda(string id) => DefinirArquivada(id, false);

    private ResultadoOperacao DefinirArquivada(string id, bool arquivada)
        => Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, id);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {id} não existe.");
            demanda["arquivada"] = arquivada;
            return ResultadoOperacao.Sucesso;
        });

    private static ResultadoOperacao? ValidarTitulo(string titulo)
        => ValidadorEntrada.TituloValido(titulo)
            ? null
            : ResultadoOperacao.Falha("Título inválido: use de 1 a 80 caracteres, sem caracteres de controle.");

    // ---------- repositórios ----------

    /// <summary>Vincula o repositório à demanda; <paramref name="openspecChange"/> é a change escolhida (ou nenhuma).</summary>
    public ResultadoOperacao VincularRepositorio(string demandaId, string pasta, string? openspecChange = null)
    {
        if (!ValidadorEntrada.PastaValida(pasta))
            return ResultadoOperacao.Falha("Nome de pasta inválido: use apenas letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.");
        if (!string.IsNullOrEmpty(openspecChange) && !ValidadorEntrada.OpenspecChangeValido(openspecChange))
            return ResultadoOperacao.Falha("Change do OpenSpec inválida: use letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarOuCriarImplicita(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (EncontrarRepositorio(demanda, pasta) is not null)
                return ResultadoOperacao.Falha($"O repositório {pasta} já está vinculado a este trabalho.");

            var novo = NovoRepositorio(pasta);
            if (!string.IsNullOrEmpty(openspecChange)) novo["openspecChange"] = openspecChange;
            Repositorios(demanda).Add(novo);
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>
    /// Grava a change do repositório (ou a remove, com <c>null</c>). Só o repositório é gravado: o campo antigo da
    /// demanda é preservado, mas o repositório passa a ter a própria change (vazia, quando "Nenhuma").
    /// </summary>
    public ResultadoOperacao DefinirChange(string demandaId, string pasta, string? openspecChange)
    {
        if (!string.IsNullOrEmpty(openspecChange) && !ValidadorEntrada.OpenspecChangeValido(openspecChange))
            return ResultadoOperacao.Falha("Change do OpenSpec inválida: use letras, dígitos, \"_\", \".\" e \"-\", e não só pontos.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null) return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");

            if (string.IsNullOrEmpty(openspecChange))
            {
                repo.Remove("openspecChange");
                // "Nenhuma" precisa vencer a change herdada da demanda: o texto vazio no repositório vale como "nenhuma".
                if (demanda["openspecChange"] is not null) repo["openspecChange"] = "";
            }
            else repo["openspecChange"] = openspecChange;
            return ResultadoOperacao.Sucesso;
        });
    }

    /// <summary>Remove o repositório e as sessões registradas nele, só nessa demanda. A confirmação é de quem chama.</summary>
    public ResultadoOperacao RemoverRepositorio(string demandaId, string pasta)
    {
        return Alterar(raiz =>
        {
            var demanda = EncontrarDemanda(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (demanda["repositorios"] is not JsonArray repos)
                return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");

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
                : ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");
        });
    }

    // ---------- sessões ----------

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
        if (!ValidadorEntrada.PastaValida(pasta)) return ResultadoOperacao.Falha("Repositório inválido.");
        if (!ValidadorEntrada.IdentificadorValido(pedido.SessionId)) return ResultadoOperacao.Falha("Identificador de sessão inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo)) return ResultadoOperacao.Falha("Rótulo inválido.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarOuCriarImplicita(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (demanda["arquivada"] is JsonValue arq && arq.TryGetValue<bool>(out var arquivada) && arquivada)
                return ResultadoOperacao.Falha($"O trabalho {demandaId} está arquivado.");

            var existente = SessoesComId(raiz, pedido.SessionId).FirstOrDefault();
            JsonObject? no = null;
            if (existente is not null)
            {
                // Só a sessão registrada em "Sessões" pode ser movida; as de um trabalho continuam onde estão.
                var implicita = EncontrarDemanda(raiz, IdDemandaImplicita);
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
        => Registrar(IdDemandaImplicita, pasta, pedido.Agente, pedido.SessionId, null, vinculadaEm, vinculadaEm, criarRepositorio: true);

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
        if (!ValidadorEntrada.PastaValida(pasta)) return ResultadoOperacao.Falha("Repositório inválido.");
        if (!ValidadorEntrada.IdentificadorValido(sessionId)) return ResultadoOperacao.Falha("Identificador de sessão inválido.");
        if (rotulo is not null && !ValidadorEntrada.RotuloValido(rotulo)) return ResultadoOperacao.Falha("Rótulo inválido.");

        return Alterar(raiz =>
        {
            var demanda = EncontrarOuCriarImplicita(raiz, demandaId);
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (vinculadaEm is not null && demanda["arquivada"] is JsonValue arq && arq.TryGetValue<bool>(out var arquivada) && arquivada)
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
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null) return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");
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
            if (demanda is null) return ResultadoOperacao.Falha($"O trabalho {demandaId} não existe.");
            if (demanda["arquivada"] is JsonValue arq && arq.TryGetValue<bool>(out var arquivada) && arquivada)
                return ResultadoOperacao.Falha($"O trabalho {demandaId} está arquivado.");
            var repo = EncontrarRepositorio(demanda, pasta);
            if (repo is null) return ResultadoOperacao.Falha($"O repositório {pasta} não está vinculado a este trabalho.");

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
            foreach (var demanda in raiz["demandas"]!.AsArray().OfType<JsonObject>())
            {
                if (demanda["repositorios"] is not JsonArray repos) continue;
                foreach (var repo in repos.OfType<JsonObject>())
                {
                    if (repo["sessoes"] is not JsonArray sessoes) continue;
                    for (var i = sessoes.Count - 1; i >= 0; i--)
                        if (sessoes[i] is JsonObject s && SessaoIgual(s, sessionId)) sessoes.RemoveAt(i);
                }
            }

            if (raiz["excluidas"] is not JsonArray lista) raiz["excluidas"] = lista = new JsonArray();
            var jaExcluida = lista.OfType<JsonObject>().Any(e =>
                string.Equals(Texto(e, "sessionId"), sessionId, StringComparison.OrdinalIgnoreCase)
                && Texto(e, "agente") == ValidadorEntrada.NomeAgente(agente));
            if (!jaExcluida)
                lista.Add(new JsonObject
                {
                    ["agente"] = ValidadorEntrada.NomeAgente(agente),
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
            if (!ValidadorEntrada.TentarAgente(Texto(e, "agente"), out var agente)) continue;
            var id = Texto(e, "sessionId");
            if (!ValidadorEntrada.IdentificadorExclusaoValido(id)) continue;
            var data = DateTimeOffset.TryParse(Texto(e, "excluidaEm"), CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d) ? d : DateTimeOffset.MinValue;
            resultado.Add(new SessaoExcluida(agente, id!, data));
        }
        return resultado;
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

    /// <summary>
    /// A demanda desse identificador; "Sessões" (<c>sem-demanda</c>) é criada se o arquivo ainda não a tiver, pois
    /// é fixa e pode receber repositórios e sessões antes de qualquer outra.
    /// </summary>
    private static JsonObject? EncontrarOuCriarImplicita(JsonObject raiz, string id)
    {
        var existente = EncontrarDemanda(raiz, id);
        if (existente is not null || !RegrasDemanda.EhImplicita(id)) return existente;
        var nova = new JsonObject
        {
            ["id"] = IdDemandaImplicita,
            ["titulo"] = TituloDemandaImplicita,
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
