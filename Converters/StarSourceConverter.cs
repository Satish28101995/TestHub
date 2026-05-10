using System.Globalization;

namespace TestHub.Converters;

/// <summary>
/// Picks between the filled and outline star icons based on a boolean
/// "is filled" flag. Used by the customer-review rating row so each
/// of the five star <see cref="Image"/>s can bind directly to its own
/// <c>StarNFilled</c> property without any code-behind logic.
/// </summary>
public sealed class StarSourceConverter : IValueConverter
{
    private static readonly ImageSource Filled  = ImageSource.FromFile("icon_star_filled.png");
    private static readonly ImageSource Outline = ImageSource.FromFile("icon_star_outline.png");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? Filled : Outline;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
