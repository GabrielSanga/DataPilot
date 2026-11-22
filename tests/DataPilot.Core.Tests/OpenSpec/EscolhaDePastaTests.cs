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

    [Theory]
    [InlineData(@"C:\Projetos\Faturamento")]
    [InlineData(@"C:\Fontes\Faturamento\src")]
    [InlineData(@"C:\Fontes")]
    [InlineData(@"C:\")]
    [InlineData(@"D:\Fontes\Faturamento")]
    [InlineData(@"C:\FontesOutra\Faturamento")]
    [InlineData(@"C:\Fontes\..\Projetos\X")]
    [InlineData("")]
    [InlineData(null)]
    public void Recusa_pasta_fora_da_raiz(string? caminho)
    {
        var r = EscolhaDePasta.Validar(Raiz, caminho);
        Assert.False(r.Ok);
        Assert.Null(r.Pasta);
        Assert.Contains(@"C:\Fontes", r.Erro);
    }

    [Theory]
    [InlineData(@"C:\Fontes\com espaço")]
    [InlineData(@"C:\Fontes\a;b")]
    public void Recusa_nome_fora_da_regra_de_pasta(string caminho)
    {
        var r = EscolhaDePasta.Validar(Raiz, caminho);
        Assert.False(r.Ok);
        Assert.Contains("Nome de pasta inválido", r.Erro);
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
