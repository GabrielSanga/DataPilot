using System.Windows.Data;

namespace DataPilot.Wpf.Converters;

/// <summary>Conversor para habilitar um controle quando todas as condições são verdadeiras.</summary>
public sealed class TodosVerdadeiros : IMultiValueConverter
{
    public static readonly TodosVerdadeiros Instancia = new();

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => values.All(v => v is true);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
