using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DataPilot.Wpf.ViewModels;

namespace DataPilot.Wpf.Views;

public partial class MainWindow : Window
{

    #region "Variáveis"

    private MainViewModel _ViewModel => (MainViewModel)DataContext;

    private bool _sincronizandoSelecao;

    #endregion

    #region "Construtor"

    public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += (_, e) =>
            {
                if (e.OldValue is INotifyPropertyChanged antigo) antigo.PropertyChanged -= VieModelPropertyChanged;
                if (e.NewValue is INotifyPropertyChanged novo) novo.PropertyChanged += VieModelPropertyChanged;
            };
        }

    #endregion

    #region "Methods"

    private void VieModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.DemandaSelecionada):
                SincronizarSelecaoDaLista();
                break;
        }
    }
    private void SincronizarSelecaoDaLista()
    {
        _sincronizandoSelecao = true;
        try
        {
            var atual = _ViewModel.DemandaSelecionada;
            if (!ReferenceEquals(ListaDemandas.SelectedItem, atual))
                ListaDemandas.SelectedItem = atual;
        }
        finally { _sincronizandoSelecao = false; }
    }

    #endregion

    #region "Events"

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (ctrl && e.Key == Key.N)
        {
            e.Handled = true;
            _ViewModel.NovaDemanda();
        }
        else if (ctrl && e.Key == Key.F)
        {
            e.Handled = true;
            CampoFiltro.Focus();
            CampoFiltro.SelectAll();
        }
    }

    private void CampoFiltro_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        _ViewModel.FiltroTexto = "";
    }

    private void Configuracoes_Click(object sender, RoutedEventArgs e) => _ViewModel.AbrirConfiguracoes();

    private void Inicio_Click(object sender, RoutedEventArgs e) => _ViewModel.DemandaSelecionada = null;

    private void NovaDemanda_Click(object sender, RoutedEventArgs e) => _ViewModel.NovaDemanda();

    private void ListaDemandas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_sincronizandoSelecao) return;
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is DemandaItem item)
            _ViewModel.DemandaSelecionada = item;
    }

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

    #endregion

}