using System.IO;
using System.Windows;

namespace DataPilot.Wpf.Services;

/// <summary>A escolha de pasta do próprio Windows (diálogo do .NET), sempre começando na raiz dos fontes.</summary>
public static class EscolhaPastaWindows
{
    public static string? Escolher(string raiz, Window? dono)
    {
        var dialogo = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Escolha a pasta do repositório",
            Multiselect = false,
        };
        if (Directory.Exists(raiz)) dialogo.InitialDirectory = raiz;
        return (dono is null ? dialogo.ShowDialog() : dialogo.ShowDialog(dono)) == true ? dialogo.FolderName : null;
    }
}
