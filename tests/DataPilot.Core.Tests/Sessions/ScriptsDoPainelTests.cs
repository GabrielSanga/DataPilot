using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using DataPilot.Core.Sessions;

namespace DataPilot.Core.Tests.Sessions;

[SupportedOSPlatform("windows")]
public class ScriptsDoPainelTests : IDisposable
{
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), "painel-scripts-" + Guid.NewGuid().ToString("N"));
    private string Pasta => Path.Combine(_raiz, "Painel");

    public ScriptsDoPainelTests() => Directory.CreateDirectory(_raiz);

    public void Dispose()
    {
        foreach (var f in Directory.EnumerateFiles(_raiz, "*", SearchOption.AllDirectories))
            File.SetAttributes(f, FileAttributes.Normal);
        // junção: apagar a pasta da junção sem seguir o destino
        Directory.Delete(_raiz, true);
    }

    private static string DoRepositorio(string nome)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "scripts", nome))) dir = dir.Parent;
        return Path.Combine(dir!.FullName, "scripts", nome);
    }

    [Theory]
    [InlineData("abrir.ps1")]
    [InlineData("hook.ps1")]
    public void Recurso_tem_os_bytes_do_arquivo_do_repositorio(string nome)
        => Assert.Equal(File.ReadAllBytes(DoRepositorio(nome)), ScriptsDoPainel.Recurso(nome));

    [Fact]
    public void Primeira_execucao_cria_a_pasta_com_os_dois_scripts_e_as_permissoes()
    {
        Assert.Null(ScriptsDoPainel.Instalar(Pasta));

        Assert.Equal(ScriptsDoPainel.Recurso("abrir.ps1"), File.ReadAllBytes(Path.Combine(Pasta, "abrir.ps1")));
        Assert.Equal(ScriptsDoPainel.Recurso("hook.ps1"), File.ReadAllBytes(Path.Combine(Pasta, "hook.ps1")));

        var seg = new DirectoryInfo(Pasta).GetAccessControl();
        Assert.True(seg.AreAccessRulesProtected);
        var usuarios = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
        var regras = seg.GetAccessRules(true, false, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>()
            .Where(r => r.IdentityReference.Equals(usuarios)).ToList();
        Assert.NotEmpty(regras);
        Assert.All(regras, r => Assert.Equal(0, (int)(r.FileSystemRights & (FileSystemRights.WriteData | FileSystemRights.Delete | FileSystemRights.ChangePermissions))));
        // o arquivo novo herdou a proteção da pasta
        Assert.True(new FileInfo(Path.Combine(Pasta, "hook.ps1")).GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier)).Count > 0);
    }

    [Fact]
    public void Script_diferente_e_substituido_e_o_igual_nao_e_regravado()
    {
        ScriptsDoPainel.Instalar(Pasta);
        var abrir = Path.Combine(Pasta, "abrir.ps1");
        var hook = Path.Combine(Pasta, "hook.ps1");
        File.WriteAllText(abrir, "# alterado");
        var data = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(hook, data);

        Assert.Null(ScriptsDoPainel.Instalar(Pasta));

        Assert.Equal(ScriptsDoPainel.Recurso("abrir.ps1"), File.ReadAllBytes(abrir));
        Assert.Equal(data, File.GetLastWriteTimeUtc(hook));
    }

    [Fact]
    public void Outros_arquivos_e_pastas_ficam_intocados()
    {
        Assert.Null(ScriptsDoPainel.Instalar(Pasta)); // a pasta criada pelo painel é a única confiável de forma previsível
        Directory.CreateDirectory(Path.Combine(Pasta, "exemplos"));
        File.WriteAllText(Path.Combine(Pasta, "exemplos", "x.json"), "{}");
        File.WriteAllText(Path.Combine(Pasta, "outro.txt"), "ok");

        Assert.Null(ScriptsDoPainel.Instalar(Pasta));

        Assert.Equal("{}", File.ReadAllText(Path.Combine(Pasta, "exemplos", "x.json")));
        Assert.Equal("ok", File.ReadAllText(Path.Combine(Pasta, "outro.txt")));
    }

    [Fact]
    public void Pasta_que_outra_conta_pode_gravar_e_recusada_sem_gravar()
    {
        Directory.CreateDirectory(Pasta);
        var info = new DirectoryInfo(Pasta);
        var seg = info.GetAccessControl();
        seg.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            FileSystemRights.Modify, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        info.SetAccessControl(seg);

        var aviso = ScriptsDoPainel.Instalar(Pasta);

        Assert.Contains("não é confiável", aviso);
        Assert.Empty(Directory.GetFileSystemEntries(Pasta));
    }

    [Fact]
    public void Juncao_e_recusada_sem_gravar()
    {
        var alvo = Path.Combine(_raiz, "alvo");
        Directory.CreateDirectory(alvo);
        var p = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{Pasta}\" \"{alvo}\"") { CreateNoWindow = true, UseShellExecute = false })!;
        p.WaitForExit();
        Assert.Equal(0, p.ExitCode);

        var aviso = ScriptsDoPainel.Instalar(Pasta);

        Assert.Contains("junção", aviso);
        Assert.Empty(Directory.GetFileSystemEntries(alvo));
        Directory.Delete(Pasta); // remove só a junção
    }

    [Fact]
    public void Sem_permissao_de_gravacao_devolve_aviso_com_icacls()
    {
        ScriptsDoPainel.Instalar(Pasta);
        var abrir = Path.Combine(Pasta, "abrir.ps1");
        File.WriteAllText(abrir, "# alterado");
        File.SetAttributes(abrir, FileAttributes.ReadOnly); // a troca por cima falha com acesso negado

        var aviso = ScriptsDoPainel.Instalar(Pasta);

        Assert.Contains("icacls", aviso);
        Assert.Equal("# alterado", File.ReadAllText(abrir));
        Assert.DoesNotContain(Directory.GetFiles(Pasta), f => f.EndsWith(".tmp"));
    }
}
