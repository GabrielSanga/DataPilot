using System.Diagnostics;
using System.Text;
using DataPilot.Core.Linking;
using DataPilot.Core.Models;
using DataPilot.Core.Monitoring;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.Monitoring;

public class LeitorPastaTests
{
    [Fact]
    public void Marcador_DefineAData_ESemEleNaoHaData()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.json", Fabrica.Json());
        p.Escrever("s2.json", Fabrica.Json(id: "s2"));
        p.Escrever("s1.criado", "");
        var inicio = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);
        File.SetCreationTimeUtc(Path.Combine(p.Caminho, "s1.criado"), inicio);

        var sessoes = new LeitorPasta(p.Caminho).Ler().Sessoes.ToDictionary(s => s.SessionId);

        Assert.Equal(new DateTimeOffset(inicio, TimeSpan.Zero), sessoes["s1"].ArquivoCriadoEm);
        Assert.Null(sessoes["s2"].ArquivoCriadoEm);

        var lancamento = new LancamentoPendente("aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa", Agente.Claude, new DateTimeOffset(inicio, TimeSpan.Zero), null);
        var demanda = new Demanda("A", "A", null, null, false, [new RepositorioCadastrado("App", [], [lancamento])]);
        var semData = sessoes["s2"] with { SessionId = "11111111-2222-3333-4444-555555555555" };
        Assert.Empty(VinculoAutomatico.Calcular([demanda], [semData], new DateTimeOffset(inicio, TimeSpan.Zero).AddHours(1)));
    }

    [Fact]
    public void Marcador_QueEAtalho_ContaComoAusente()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.json", Fabrica.Json());
        // Junção não exige privilégio, ao contrário de um link de arquivo.
        var alvo = Directory.CreateDirectory(Path.Combine(p.Caminho, "alvo")).FullName;
        var link = Path.Combine(p.Caminho, "s1.criado");
        var mklink = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{alvo}\"")
            { CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
        mklink.WaitForExit();
        Assert.Equal(0, mklink.ExitCode);

        Assert.Null(Assert.Single(new LeitorPasta(p.Caminho).Ler().Sessoes).ArquivoCriadoEm);
    }

    [Fact]
    public void Marcador_SemArquivoDeStatus_NaoGeraSessao()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.criado", "");
        Assert.Empty(new LeitorPasta(p.Caminho).Ler().Sessoes);
    }

    [Fact]
    public void ArquivoValido_GeraSessao()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.json", Fabrica.Json());
        var r = new LeitorPasta(p.Caminho).Ler();
        Assert.True(r.Disponivel);
        Assert.Equal("s1", Assert.Single(r.Sessoes).SessionId);
    }

    [Fact]
    public void PastaVazia_SemSessoes_MasDisponivel()
    {
        using var p = new PastaTemporaria();
        var r = new LeitorPasta(p.Caminho).Ler();
        Assert.True(r.Disponivel);
        Assert.Empty(r.Sessoes);
    }

    [Fact]
    public void OutrasExtensoes_Ignoradas()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.123.tmp", Fabrica.Json());
        p.Escrever("s2.txt", Fabrica.Json(id: "s2"));
        p.Escrever("s3.jsonx", Fabrica.Json(id: "s3"));
        Assert.Empty(new LeitorPasta(p.Caminho).Ler().Sessoes);
    }

    [Fact]
    public void ArquivoInvalido_NaoDerrubaOsDemais()
    {
        using var p = new PastaTemporaria();
        p.Escrever("ruim.json", "isto nao e json");
        p.Escrever("s1.json", Fabrica.Json());
        Assert.Equal("s1", Assert.Single(new LeitorPasta(p.Caminho).Ler().Sessoes).SessionId);
    }

    [Fact]
    public void ArquivoComBom_Aceito()
    {
        using var p = new PastaTemporaria();
        File.WriteAllText(Path.Combine(p.Caminho, "s1.json"), Fabrica.Json(), new UTF8Encoding(true));
        Assert.Single(new LeitorPasta(p.Caminho).Ler().Sessoes);
    }

    [Fact]
    public void Utf8Invalido_Descartado()
    {
        using var p = new PastaTemporaria();
        File.WriteAllBytes(Path.Combine(p.Caminho, "s1.json"), [0xFF, 0xFE, 0x7B, 0x80]);
        Assert.Empty(new LeitorPasta(p.Caminho).Ler().Sessoes);
    }

    [Fact]
    public void ArquivoMaiorQue16KB_Descartado_NoLimiteAceito()
    {
        using var p = new PastaTemporaria();
        // Preenche com espaços até exatamente 16 KB (JSON continua válido) e depois 1 byte além.
        var baseJson = Fabrica.Json();
        p.Escrever("s1.json", baseJson + new string(' ', Constantes.TamanhoMaximoArquivo - baseJson.Length));
        var grande = Fabrica.Json(id: "s2");
        p.Escrever("s2.json", grande + new string(' ', Constantes.TamanhoMaximoArquivo - grande.Length + 1));
        var r = new LeitorPasta(p.Caminho).Ler();
        Assert.Equal("s1", Assert.Single(r.Sessoes).SessionId);
    }

    [Fact]
    public void ArquivoEnormeCompleto_Descartado()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.json", Fabrica.Json() + new string(' ', 5 * 1024 * 1024));
        Assert.Empty(new LeitorPasta(p.Caminho).Ler().Sessoes);
    }

    [Fact]
    public void MaisDe500Arquivos_LeApenasOs500MaisRecentes()
    {
        using var p = new PastaTemporaria();
        var baseTempo = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 520; i++)
        {
            var id = $"s{i:D3}";
            var caminho = p.Escrever(id + ".json", Fabrica.Json(id: id));
            File.SetLastWriteTimeUtc(caminho, baseTempo.AddMinutes(i));
        }
        var ids = new LeitorPasta(p.Caminho).Ler().Sessoes.Select(s => s.SessionId).ToHashSet();
        Assert.Equal(500, ids.Count);
        Assert.DoesNotContain("s000", ids);
        Assert.DoesNotContain("s019", ids);
        Assert.Contains("s020", ids);
        Assert.Contains("s519", ids);
    }

    [Fact]
    public void ArquivoRemovido_ASessaoSome()
    {
        using var p = new PastaTemporaria();
        var caminho = p.Escrever("s1.json", Fabrica.Json());
        var leitor = new LeitorPasta(p.Caminho);
        Assert.Single(leitor.Ler().Sessoes);
        File.Delete(caminho);
        Assert.Empty(leitor.Ler().Sessoes);
    }

    [Fact]
    public void ArquivoAlterado_ReflitaNovoConteudo()
    {
        using var p = new PastaTemporaria();
        var caminho = p.Escrever("s1.json", Fabrica.Json(status: "executando"));
        var leitor = new LeitorPasta(p.Caminho);
        Assert.Equal(StatusGravado.Executando, leitor.Ler().Sessoes[0].Status);
        p.Escrever("s1.json", Fabrica.Json(status: "atencao"));
        File.SetLastWriteTimeUtc(caminho, DateTime.UtcNow.AddSeconds(5));
        Assert.Equal(StatusGravado.Atencao, leitor.Ler().Sessoes[0].Status);
    }

    [Fact]
    public void PastaInexistente_Indisponivel()
    {
        using var p = new PastaTemporaria(criar: false);
        var r = new LeitorPasta(p.Caminho).Ler();
        Assert.False(r.Disponivel);
        Assert.Empty(r.Sessoes);
    }

    [Fact]
    public void CaminhoQueEUmArquivo_Indisponivel()
    {
        using var p = new PastaTemporaria();
        var arquivo = p.Escrever("x.txt", "oi");
        Assert.False(new LeitorPasta(arquivo).Ler().Disponivel);
    }

    [Fact]
    public void ArquivoAbertoSemCompartilhar_AfetaSoEsseArquivo()
    {
        using var p = new PastaTemporaria();
        var travado = p.Escrever("s1.json", Fabrica.Json());
        p.Escrever("s2.json", Fabrica.Json(id: "s2"));
        using var trava = new FileStream(travado, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var r = new LeitorPasta(p.Caminho).Ler();
        Assert.True(r.Disponivel);
        Assert.Equal("s2", Assert.Single(r.Sessoes).SessionId);
    }

    [Fact]
    public void CwdEstranho_ApenasLido_NaoAcessado()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.json", Fabrica.Json(cwd: "//servidor-inexistente/share/malware.exe"));
        var s = Assert.Single(new LeitorPasta(p.Caminho).Ler().Sessoes);
        Assert.Contains("malware.exe", s.Cwd);
    }

    [Fact]
    public void SubstituicaoAtomicaDuranteALeitura_Conclui_ELeitorVeConteudoCompleto()
    {
        using var p = new PastaTemporaria();
        var destino = p.Escrever("s1.json", Fabrica.Json(status: "executando"));
        var tmp = Path.Combine(p.Caminho, "s1.999.tmp");
        File.WriteAllText(tmp, Fabrica.Json(status: "atencao"), new UTF8Encoding(false));

        // Abre como o painel abre (compartilhando leitura, escrita e exclusão) e mantém aberto durante a troca.
        using (var aberto = new FileStream(destino, new FileStreamOptions
        {
            Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.ReadWrite | FileShare.Delete,
        }))
        {
            File.Replace(tmp, destino, null); // mesma troca que o hook faz
            var lido = new StreamReader(aberto, new UTF8Encoding(false)).ReadToEnd();
            Assert.True(ValidadorStatus.TentarLer(lido, "s1", out var anterior));
            Assert.Equal(StatusGravado.Executando, anterior!.Status);
        }
        Assert.Equal(StatusGravado.Atencao, new LeitorPasta(p.Caminho).Ler().Sessoes[0].Status);
    }

    [Fact]
    public void DataDeCriacao_VemDoMarcador_ENaoDoUltimoEvento()
    {
        using var p = new PastaTemporaria();
        var marcador = p.Escrever("s1.criado", "");
        var criacao = new DateTime(2026, 11, 10, 8, 30, 0, DateTimeKind.Utc);
        File.SetCreationTimeUtc(marcador, criacao);
        var caminho = p.Escrever("s1.json", Fabrica.Json());
        // Evento posterior: muda a modificação e o conteúdo do arquivo de status.
        File.WriteAllText(caminho, Fabrica.Json(status: "aguardando", atualizado: "2026-11-14T10:00:00-03:00"));
        File.SetLastWriteTimeUtc(caminho, criacao.AddDays(4));

        var sessao = Assert.Single(new LeitorPasta(p.Caminho).Ler().Sessoes);
        Assert.Equal(new DateTimeOffset(criacao, TimeSpan.Zero), sessao.ArquivoCriadoEm);
    }

    [Fact]
    public void DataDeCriacao_DeSessaoRecemCriada_EOMomentoEmQueOMarcadorSurgiu()
    {
        using var p = new PastaTemporaria();
        var antes = DateTime.UtcNow.AddSeconds(-2);
        p.Escrever("s1.criado", "");
        p.Escrever("s1.json", Fabrica.Json());
        var depois = DateTime.UtcNow.AddSeconds(2);

        var criada = Assert.Single(new LeitorPasta(p.Caminho).Ler().Sessoes).ArquivoCriadoEm;
        Assert.NotNull(criada);
        Assert.InRange(criada!.Value.UtcDateTime, antes, depois);
    }

    [Fact]
    public void DataDeCriacao_AtualizaQuandoOMarcadorMuda_SemMudarOArquivo()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.json", Fabrica.Json());
        var leitor = new LeitorPasta(p.Caminho);
        Assert.Null(leitor.Ler().Sessoes[0].ArquivoCriadoEm);

        var criacao = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetCreationTimeUtc(p.Escrever("s1.criado", ""), criacao);

        Assert.Equal(new DateTimeOffset(criacao, TimeSpan.Zero), leitor.Ler().Sessoes[0].ArquivoCriadoEm);
    }

    [Fact]
    public void LeituraENaoMonitoramento_NaoAlteramAPasta()
    {
        using var p = new PastaTemporaria();
        p.Escrever("s1.json", Fabrica.Json());
        p.Escrever("ruim.json", "x");
        p.Escrever("s1.1.tmp", "t");
        var antes = Retrato(p.Caminho);

        new LeitorPasta(p.Caminho).Ler();
        using (var m = new MonitorPasta(p.Caminho, TimeSpan.FromMilliseconds(100)))
        {
            var n = 0;
            m.RetratoAtualizado += _ => Interlocked.Increment(ref n);
            m.Iniciar();
            Assert.True(Espera.Ate(() => Volatile.Read(ref n) >= 3, TimeSpan.FromSeconds(5)));
        }

        Assert.Equal(antes, Retrato(p.Caminho));
    }

    private static string[] Retrato(string pasta) =>
        Directory.EnumerateFileSystemEntries(pasta).OrderBy(x => x)
            .Select(f => $"{Path.GetFileName(f)}|{new FileInfo(f).Length}|{File.GetLastWriteTimeUtc(f).Ticks}|{File.ReadAllText(f)}")
            .ToArray();
}
