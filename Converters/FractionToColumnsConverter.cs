using System.Globalization;

namespace TestHub.Converters;

/// <summary>
/// Turns a 0..1 fraction into a two-column <see cref="ColumnDefinitionCollection"/>
/// that the milestone-progress Grid uses to size its filled bar exactly to
/// <c>completedMilestones / totalMilestones</c>. Star sizing means the Grid
/// itself does the math at layout time, so it works reliably inside virtualized
/// CollectionView cells (where MultiBinding against <c>Width</c> is flaky).
/// </summary>
public sealed class FractionToColumnsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fraction = ToDouble(value);
        if (double.IsNaN(fraction))
        {
            fraction = 0d;
        }
        fraction = Math.Clamp(fraction, 0d, 1d);

        // Grid star sizing: completed / remaining. Edge-case zero values are
        // emitted as Absolute 0 so the unused side collapses cleanly to nothing.
        var first  = fraction <= 0d
            ? new GridLength(0, GridUnitType.Absolute)
            : new GridLength(fraction, GridUnitType.Star);
        var second = fraction >= 1d
            ? new GridLength(0, GridUnitType.Absolute)
            : new GridLength(1d - fraction, GridUnitType.Star);

        return new ColumnDefinitionCollection
        {
            new ColumnDefinition { Width = first },
            new ColumnDefinition { Width = second },
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static double ToDouble(object? v) => v switch
    {
        double d => d,
        float f => f,
        decimal m => (double)m,
        int i => i,
        string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var p) => p,
        _ => 0d,
    };
}
