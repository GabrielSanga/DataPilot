using System.Diagnostics;
using DataPilot.Core.Arquivos;
using DataPilot.Core.Models;
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

    private static ResultadoPasta ComoPasta(string caminho, LeituraSegura.TipoEntrada tipo = LeituraSegura.TipoEntrada.Normal)
        => EscolhaDePasta.Validar(Raiz, caminho, _ => tipo);

    [Theory]
    [InlineData(@"C:\Fontes\Grupo\Faturamento")]
    [InlineData(@"C:\Fontes\com espaço")]
    [InlineData(@"C:\openspec\ERP\Faturamento")]
    [InlineData(@"C:\openspec\ERP")]
    [InlineData(@"C:\Fontes")]
    public void Aceita_pasta_dos_locais_do_container_com_o_caminho_completo(string caminho)
    {
        var r = ComoPasta(caminho + @"\");
        Assert.True(r.Ok, r.Erro);
        Assert.Equal(caminho, r.Pasta);
    }

    [Theory]
    [InlineData(@"C:\Projetos\Faturamento")]
    [InlineData(@"D:\Faturamento")]
    [InlineData(@"C:\FontesAntigas\X")]
    [InlineData(@"C:\openspec")]
    [InlineData(@"C:\Fontes\..\Projetos")]
    [InlineData(@"C:\")]
    [InlineData(@"D:\")]
    public void Recusa_pasta_fora_dos_locais_do_container(string caminho)
    {
        var r = ComoPasta(caminho);
        Assert.False(r.Ok);
        Assert.Null(r.Pasta);
        Assert.Equal(LocaisContainer.MensagemFora, r.Erro);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Recusa_caminho_vazio(string? caminho)
    {
        var r = EscolhaDePasta.Validar(Raiz, caminho);
        Assert.False(r.Ok);
        Assert.Equal(DataPilot.Core.Validation.ValidadorEntrada.MensagemRepositorio, r.Erro);
    }

    [Fact]
    public void Recusa_pasta_que_nao_existe()
    {
        var r = EscolhaDePasta.Validar(Raiz, @"C:\Fontes\Grupo\datapilot-" + Guid.NewGuid().ToString("N"));
        Assert.False(r.Ok);
        Assert.Equal(EscolhaDePasta.MensagemInexistente, r.Erro);
    }

    [Fact]
    public void Recusa_atalho_ou_juncao_nos_locais()
    {
        var r = ComoPasta(@"C:\Fontes\Grupo\juncao", LeituraSegura.TipoEntrada.Atalho);
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
