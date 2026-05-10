using System.Globalization;

namespace TestHub.Converters;

/// <summary>
/// Multi-binding converter used to compute a child's <c>WidthRequest</c>
/// as <c>parentWidth × fraction</c>. Used by the gradient milestone-progress
/// bar on the Contracts list — bind both the track's measured Width and the
/// 0..1 ProgressFraction and the converter returns the filled bar's pixel
/// width that lays out cleanly inside a 0-padded Grid.
/// </summary>
public sealed class WidthMultiplyConverter : IMultiValueConverter
{
    public object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
        {
            return 0d;
        }

        var width = ToDouble(values[0]);
        var fraction = ToDouble(values[1]);

        if (double.IsNaN(width) || width <= 0)
        {
            return 0d;
        }

        return Math.Max(0d, width * Math.Clamp(fraction, 0d, 1d));
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static double ToDouble(object? v) => v switch
    {
        double d => d,
        float f => f,
        int i => i,
        decimal m => (double)m,
        string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var p) => p,
        _ => 0d,
    };
}
