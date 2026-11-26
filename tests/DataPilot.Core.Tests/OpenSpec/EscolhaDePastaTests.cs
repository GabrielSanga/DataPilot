using System.Diagnostics;
using DataPilot.Core.OpenSpec;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.OpenSpec;

public class EscolhaDePastaTests
{
    private const string Raiz = @"C:\Fontes";

    [Theory]
    [InlineData(@"C:\Fontes\Faturamento", "Faturamento")]
    [InlineData(@"C:\Fontes\Faturamento\", "Faturamento")]
    [InlineData(@"c:\fontes\Estoque", "Estoque")]
    [InlineData(@"C:\Fontes\app.v2", "app.v2")]
    public void Aceita_pasta_direta_da_raiz_e_devolve_so_o_nome(string caminho, string nome)
    {
        var r = EscolhaDePasta.Validar(Raiz, caminho);
        Assert.True(r.Ok);
        Assert.Equal(nome, r.Pasta);
    }

    [Fact]
    public void Aceita_pasta_fora_da_raiz_e_devolve_o_caminho_completo()
    {
        using var pasta = new PastaTemporaria();
        var repo = Directory.CreateDirectory(Path.Combine(pasta.Caminho, "meu repo & cia")).FullName;

        var r = EscolhaDePasta.Validar(Raiz, repo + @"\");

        Assert.True(r.Ok);
        Assert.Equal(repo, r.Pasta);
    }

    [Fact]
    public void Pasta_da_raiz_com_nome_fora_da_regra_vira_caminho_completo()
    {
        using var raiz = new PastaTemporaria();
        var repo = Directory.CreateDirectory(Path.Combine(raiz.Caminho, "com espaço")).FullName;

        var r = EscolhaDePasta.Validar(raiz.Caminho, repo);

        Assert.True(r.Ok);
        Assert.Equal(repo, r.Pasta);
    }

    [Theory]
    [InlineData(@"C:\")]
    [InlineData("")]
    [InlineData(null)]
    public void Recusa_caminho_invalido(string? caminho)
    {
        var r = EscolhaDePasta.Validar(Raiz, caminho);
        Assert.False(r.Ok);
        Assert.Null(r.Pasta);
        Assert.Equal(DataPilot.Core.Validation.ValidadorEntrada.MensagemRepositorio, r.Erro);
    }

    [Fact]
    public void Recusa_pasta_que_nao_existe()
    {
        var r = EscolhaDePasta.Validar(Raiz, Path.Combine(Path.GetTempPath(), "datapilot-" + Guid.NewGuid().ToString("N")));
        Assert.False(r.Ok);
        Assert.Equal(EscolhaDePasta.MensagemInexistente, r.Erro);
    }

    [Fact]
    public void Recusa_juncao_fora_da_raiz()
    {
        using var pasta = new PastaTemporaria();
        var alvo = Directory.CreateDirectory(Path.Combine(pasta.Caminho, "alvo")).FullName;
        var link = Path.Combine(pasta.Caminho, "juncao");
        var p = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{alvo}\"")
            { CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
        p.WaitForExit();
        Assert.Equal(0, p.ExitCode);

        var r = EscolhaDePasta.Validar(Raiz, link);
        Assert.False(r.Ok);
        Assert.Equal(EscolhaDePasta.MensagemAtalho, r.Erro);
    }

    [Fact]
    public void PastasDaRaiz_ignora_atalho_nome_invalido_e_arquivo_e_ordena()
    {
        using var raiz = new PastaTemporaria();
        foreach (var nome in new[] { "beta", "Alfa", "com espaço", "a;b" })
            Directory.CreateDirectory(Path.Combine(raiz.Caminho, nome));
        raiz.Escrever("arquivo.txt", "x");
        var alvo = Path.Combine(raiz.Caminho, "beta");
        var link = Path.Combine(raiz.Caminho, "atalho");
        var p = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{alvo}\"")
            { CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
        p.WaitForExit();
        Assert.Equal(0, p.ExitCode);

        Assert.Equal(["Alfa", "beta"], EscolhaDePasta.PastasDaRaiz(raiz.Caminho));
    }

    [Fact]
    public void PastasDaRaiz_de_raiz_inexistente_devolve_vazio()
        => Assert.Empty(EscolhaDePasta.PastasDaRaiz(Path.Combine(Path.GetTempPath(), "datapilot-" + Guid.NewGuid().ToString("N"))));
}
