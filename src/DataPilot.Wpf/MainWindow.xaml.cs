using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DataPilot.Wpf;

public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext;

    // A lista e o view model se atualizam por eventos separados; este sinalizador evita que um eco volte ao outro.
    private bool _sincronizandoSelecao;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is INotifyPropertyChanged antigo) antigo.PropertyChanged -= VmPropertyChanged;
            if (e.NewValue is INotifyPropertyChanged novo) novo.PropertyChanged += VmPropertyChanged;
        };
    }

    private void VmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.DemandaSelecionada):
                SincronizarSelecaoDaLista();
                break;
        }
    }

    // ---------- seleção da lista ----------

    /// <summary>
    /// Só uma escolha do usuário (um item que entra na seleção) muda a demanda selecionada. Quando o filtro esconde o
    /// item selecionado a lista fica sem seleção, mas a demanda continua sendo a mesma e a área dela não muda.
    /// </summary>
    private void ListaDemandas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_sincronizandoSelecao) return;
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is DemandaItem item)
            Vm.DemandaSelecionada = item;
    }

    private void SincronizarSelecaoDaLista()
    {
        _sincronizandoSelecao = true;
        try
        {
            var atual = Vm.DemandaSelecionada;
            if (!ReferenceEquals(ListaDemandas.SelectedItem, atual))
                ListaDemandas.SelectedItem = atual;
        }
        finally { _sincronizandoSelecao = false; }
    }

    // ---------- teclado ----------

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (ctrl && e.Key == Key.N)
        {
            e.Handled = true;
            Vm.NovaDemanda();
        }
        else if (ctrl && e.Key == Key.F)
        {
            e.Handled = true;
            CampoFiltro.Focus();
            CampoFiltro.SelectAll();
        }
        else if (e.Key == Key.F8 && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            if (Vm.IrParaProximaCommand.CanExecute(null)) Vm.IrParaProximaCommand.Execute(null);
        }
    }

    /// <summary>Esc no filtro limpa o texto.</summary>
    private void CampoFiltro_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        Vm.FiltroTexto = "";
    }

    private void NovaDemanda_Click(object sender, RoutedEventArgs e) => Vm.NovaDemanda();

    // ---------- menus "Mais ações" ----------

    /// <summary>
    /// Abre o menu da linha, do repositório ou da demanda a que o botão pertence. O mesmo menu abre com o botão
    /// direito, com a tecla de menu e com Shift+F10 sobre a linha (isso é do próprio WPF).
    /// </summary>
    private void MaisAcoes_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement botao) return;

        DependencyObject? atual = VisualTreeHelper.GetParent(botao);
        while (atual is not null)
        {
            if (atual is FrameworkElement dono && dono.ContextMenu is { } menu)
            {
                var ponto = botao.TranslatePoint(new Point(0, botao.ActualHeight), dono);
                menu.PlacementTarget = dono;
                menu.Placement = PlacementMode.Relative;
                menu.HorizontalOffset = ponto.X;
                menu.VerticalOffset = ponto.Y;
                // O menu é compartilhado: depois de fechado, a posição volta ao padrão (teclado e botão direito).
                RoutedEventHandler? abriu = null;
                RoutedEventHandler? fechou = null;
                // O menu alinha a borda direita à do botão, que fica rente à borda da janela; a largura só se conhece ao abrir.
                abriu = (_, _) =>
                {
                    menu.Opened -= abriu;
                    menu.HorizontalOffset = Math.Max(0, ponto.X + botao.ActualWidth - menu.ActualWidth);
                };
                fechou = (_, _) =>
                {
                    menu.Closed -= fechou;
                    menu.ClearValue(ContextMenu.PlacementProperty);
                    menu.ClearValue(ContextMenu.HorizontalOffsetProperty);
                    menu.ClearValue(ContextMenu.VerticalOffsetProperty);
                };
                menu.Opened += abriu;
                menu.Closed += fechou;
                menu.IsOpen = true;
                e.Handled = true;
                return;
            }
            atual = VisualTreeHelper.GetParent(atual);
        }
    }
}
