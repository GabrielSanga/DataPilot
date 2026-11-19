using System.Diagnostics;
using DataPilot.Core.OpenSpec;
using DataPilot.Core.Tests.Helpers;

namespace DataPilot.Core.Tests.OpenSpec;

public class ChangesDoRepositorioTests : IDisposable
{
    private readonly PastaTemporaria _raiz = new();

    public void Dispose() => _raiz.Dispose();

    private string Pasta(params string[] partes) => Path.Combine([_raiz.Caminho, .. partes]);

    private void Change(string repo, string nome) => Directory.CreateDirectory(Pasta(repo, "openspec", "changes", nome));

    private static void Juncao(string link, string alvo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        var p = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{alvo}\"")
            { CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
        p.WaitForExit();
        Assert.Equal(0, p.ExitCode);
    }

    [Fact]
    public void Lista_as_changes_ativas_em_ordem_sem_archive()
    {
        Change("F", "nova-api");
        Change("F", "ajuste-boleto");
        Change("F", "archive");
        Assert.Equal(["ajuste-boleto", "nova-api"], ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
    }

    [Fact]
    public void Ignora_arquivos_e_nomes_invalidos()
    {
        Change("F", "boa");
        Change("F", "nome com espaco");
        Change("F", "a;b");
        File.WriteAllText(Pasta("F", "openspec", "changes", "arquivo.md"), "x");
        Assert.Equal(["boa"], ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
    }

    [Fact]
    public void Sem_openspec_changes_a_lista_e_vazia()
    {
        Directory.CreateDirectory(Pasta("F"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "Inexistente"));
    }

    [Fact]
    public void Pasta_invalida_devolve_lista_vazia()
        => Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, ".."));

    [Fact]
    public void Limita_a_200_changes()
    {
        for (var i = 0; i < 205; i++) Change("F", $"c{i:000}");
        var lista = ChangesDoRepositorio.Listar(_raiz.Caminho, "F");
        Assert.Equal(200, lista.Count);
        Assert.Equal("c000", lista[0]);
    }

    [Fact]
    public void Entrada_que_e_juncao_nao_aparece()
    {
        Change("F", "boa");
        Directory.CreateDirectory(Pasta("Outro", "alvo"));
        Juncao(Pasta("F", "openspec", "changes", "atalho"), Pasta("Outro", "alvo"));
        Assert.Equal(["boa"], ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
    }

    [Fact]
    public void Changes_ou_openspec_que_e_juncao_recusa_tudo()
    {
        Directory.CreateDirectory(Pasta("Outro", "changes", "x"));
        Directory.CreateDirectory(Pasta("F", "openspec"));
        Juncao(Pasta("F", "openspec", "changes"), Pasta("Outro", "changes"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));

        Directory.CreateDirectory(Pasta("Alvo", "changes", "y"));
        Juncao(Pasta("G", "openspec"), Pasta("Alvo"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "G"));
    }

    [Fact]
    public void Repositorio_que_e_juncao_recusa_tudo()
    {
        Directory.CreateDirectory(Pasta("Outro", "openspec", "changes", "x"));
        Juncao(Pasta("F"), Pasta("Outro"));
        Assert.Empty(ChangesDoRepositorio.Listar(_raiz.Caminho, "F"));
    }
}
