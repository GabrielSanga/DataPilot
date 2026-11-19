using DataPilot.Core.OpenSpec;

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
}
