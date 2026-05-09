using System.Globalization;

namespace TestHub.Converters;

/// <summary>
/// Returns the boolean negation of the bound value. Used to flip
/// IsBusy into IsNotBusy for visibility bindings.
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}
