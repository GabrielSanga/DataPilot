using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using DataPilot.Core.Models;

namespace DataPilot.Core.Monitoring;

/// <summary>Valida o conteúdo (não confiável) de um arquivo de status.</summary>
public static partial class ValidadorStatus
{
    [GeneratedRegex(@"^[A-Za-z0-9_-]{1,100}$")]
    private static partial Regex PadraoId();

    [GeneratedRegex(@"(Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex PadraoFuso();

    private static readonly JsonDocumentOptions Opcoes = new() { MaxDepth = 8 };

    public static bool TentarLer(string conteudo, string nomeArquivoSemExtensao, [NotNullWhen(true)] out Sessao? sessao)
    {
        sessao = null;
        try
        {
            using var doc = JsonDocument.Parse(conteudo, Opcoes);
            var raiz = doc.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object) return false;

            if (!Texto(raiz, "agente", out var agenteTxt) || !Agentes.TentarLer(agenteTxt, out var agente)) return false;

            if (!Texto(raiz, "session_id", out var id)) return false;
            if (!PadraoId().IsMatch(id) || id != nomeArquivoSemExtensao) return false;

            if (!Texto(raiz, "status", out var statusTxt)) return false;
            StatusGravado status;
            switch (statusTxt)
            {
                case "ativa": status = StatusGravado.Ativa; break;
                case "executando": status = StatusGravado.Executando; break;
                case "aguardando": status = StatusGravado.Aguardando; break;
                case "atencao": status = StatusGravado.Atencao; break;
                case "encerrada": status = StatusGravado.Encerrada; break;
                default: return false;
            }

            if (!Texto(raiz, "atualizado", out var dataTxt)) return false;
            if (!PadraoFuso().IsMatch(dataTxt)) return false;
            if (!DateTimeOffset.TryParse(dataTxt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var atualizado))
                return false;

            var cwd = raiz.TryGetProperty("cwd", out var c) && c.ValueKind == JsonValueKind.String
                ? c.GetString() ?? "" : "";

            sessao = new Sessao(agente, id, cwd, status, atualizado);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool Texto(JsonElement raiz, string nome, [NotNullWhen(true)] out string? valor)
    {
        valor = null;
        if (!raiz.TryGetProperty(nome, out var e) || e.ValueKind != JsonValueKind.String) return false;
        valor = e.GetString();
        return valor is not null;
    }
}
