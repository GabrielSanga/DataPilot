using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DataPilot.Core.Sessions;

/// <summary>Instala e atualiza o abrir.ps1 e o hook.ps1 embutidos, sem nunca gravar numa pasta que outra conta controle.</summary>
[SupportedOSPlatform("windows")]
public static class ScriptsDoPainel
{
    private static readonly string[] Scripts = ["abrir.ps1", "hook.ps1"];

    private const FileSystemRights Gravacao = FileSystemRights.WriteData | FileSystemRights.AppendData
        | FileSystemRights.WriteExtendedAttributes | FileSystemRights.WriteAttributes | FileSystemRights.Delete
        | FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership;

    /// <summary>Devolve o aviso a mostrar, ou null quando os scripts estão em dia.</summary>
    public static string? Instalar(string pasta)
    {
        try
        {
            var atual = WindowsIdentity.GetCurrent().User!;
            if (!Directory.Exists(pasta)) Criar(pasta, atual);
            // Confere também depois de criar: se outra conta criou a pasta antes, Create não falha.
            if (Verificar(pasta, atual) is { } motivo) return $"Os scripts do painel não foram atualizados: {motivo}";

            foreach (var nome in Scripts) Gravar(pasta, nome);
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return $"Não foi possível atualizar os scripts em {pasta}: sem permissão de gravação. Como administrador, rode: "
                + $"icacls {pasta} /grant \"{WindowsIdentity.GetCurrent().Name}:(OI)(CI)M\"";
        }
        catch (Exception ex) when (ex is IOException or System.Security.SecurityException)
        {
            return $"Não foi possível atualizar os scripts em {pasta}: {ex.Message}";
        }
    }

    internal static byte[] Recurso(string nome)
    {
        using var s = typeof(ScriptsDoPainel).Assembly.GetManifestResourceStream(nome)
            ?? throw new InvalidOperationException($"Recurso ausente: {nome}");
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    private static void Criar(string pasta, SecurityIdentifier atual)
    {
        var seg = new DirectorySecurity();
        seg.SetAccessRuleProtection(true, false);
        void Regra(SecurityIdentifier sid, FileSystemRights d) => seg.AddAccessRule(new FileSystemAccessRule(
            sid, d, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        Regra(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl);
        Regra(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl);
        Regra(atual, FileSystemRights.Modify);
        Regra(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), FileSystemRights.ReadAndExecute);
        new DirectoryInfo(pasta).Create(seg);
    }

    /// <summary>Devolve o motivo da recusa, ou null se a pasta é confiável.</summary>
    private static string? Verificar(string pasta, SecurityIdentifier atual)
    {
        var info = new DirectoryInfo(pasta);
        if (info.Attributes.HasFlag(FileAttributes.ReparsePoint)) return $"{pasta} é um atalho ou junção, então não é confiável.";

        HashSet<SecurityIdentifier> aceitos =
        [
            atual,
            new(WellKnownSidType.LocalSystemSid, null),
            new(WellKnownSidType.BuiltinAdministratorsSid, null),
            new(WellKnownSidType.CreatorOwnerSid, null), // só vale para o dono, já verificado abaixo
        ];
        var seg = info.GetAccessControl();
        if (seg.GetOwner(typeof(SecurityIdentifier)) is not SecurityIdentifier dono || !aceitos.Contains(dono))
            return $"o dono de {pasta} não é o sistema, os administradores nem o usuário atual.";
        foreach (FileSystemAccessRule r in seg.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            if (r.AccessControlType == AccessControlType.Allow && (r.FileSystemRights & Gravacao) != 0
                && !aceitos.Contains((SecurityIdentifier)r.IdentityReference))
                return $"outra conta ({r.IdentityReference.Translate(typeof(NTAccount))}) pode gravar em {pasta}, então a pasta não é confiável.";

        foreach (var nome in Scripts)
            if (File.Exists(Path.Combine(pasta, nome)) && File.GetAttributes(Path.Combine(pasta, nome)).HasFlag(FileAttributes.ReparsePoint))
                return $"{nome} é um atalho, então não é confiável.";
        return null;
    }

    private static void Gravar(string pasta, string nome)
    {
        var destino = Path.Combine(pasta, nome);
        var bytes = Recurso(nome);
        if (File.Exists(destino) && File.ReadAllBytes(destino).AsSpan().SequenceEqual(bytes)) return;

        // Temporário + troca: o arquivo novo herda a permissão da pasta e o hook nunca lê meio arquivo.
        var temp = Path.Combine(pasta, $".{nome}.tmp");
        try
        {
            File.WriteAllBytes(temp, bytes);
            File.Move(temp, destino, overwrite: true);
        }
        finally { try { File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
}
