using System.Globalization;

namespace TestHub.Converters;

/// <summary>
/// Maps a boolean to an opacity value: <c>true</c> → 1.0 (fully opaque),
/// <c>false</c> → 0.55 (visibly dimmed). Used to gray out controls
/// that are disabled but still rendered in place.
/// </summary>
/// <remarks>
/// Optionally accepts a <c>parameter</c> string of the form
/// <c>"on,off"</c> (e.g. <c>"1,0.4"</c>) to override the default ramp.
/// </remarks>
public sealed class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var on = 1.0;
        var off = 0.55;

        if (parameter is string spec && spec.Contains(',', StringComparison.Ordinal))
        {
            var parts = spec.Split(',', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedOn) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedOff))
            {
                on = parsedOn;
                off = parsedOff;
            }
        }

        return value is bool b && b ? on : off;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
