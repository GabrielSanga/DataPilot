using System.Windows;

namespace DataPilot.Wpf;

/// <summary>Perguntas e avisos ao usuário, atrás de uma interface para a lógica do view model não depender de janelas.</summary>
public interface IInteracao
{
    bool Confirmar(string mensagem);
    void Erro(string mensagem);
    void Informar(string mensagem);
}

public sealed class InteracaoMessageBox : IInteracao
{
    private const string Titulo = "DataPilot";

    public bool Confirmar(string mensagem)
        => MessageBox.Show(mensagem, Titulo, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    public void Erro(string mensagem) => MessageBox.Show(mensagem, Titulo, MessageBoxButton.OK, MessageBoxImage.Error);

    public void Informar(string mensagem) => MessageBox.Show(mensagem, Titulo, MessageBoxButton.OK, MessageBoxImage.Information);
}
