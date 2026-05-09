namespace TestHub.Models.Contractor;

/// <summary>
/// Lightweight DTO used by the dashboard's "Recent Projects" list. The
/// extra display properties (badge colors) are pre-computed from the
/// <see cref="Status"/> so the XAML can bind directly without converters.
/// </summary>
public sealed class RecentProjectItem
{
    public required string Title { get; init; }
    public required string ClientName { get; init; }
    public required string DateLabel { get; init; }
    public required string Amount { get; init; }
    public required string DateString { get; init; }
    public required string ProjectType { get; init; }

    /// <summary>active | completed | pending</summary>
    public required string Status { get; init; }

    public string BadgeText => Status switch
    {
        "active"    => "active",
        "completed" => "completed",
        "pending"   => "pending",
        _           => Status,
    };

    public Color BadgeBackground => Status switch
    {
        "active"    => Color.FromArgb("#EEF2F6"),
        "completed" => Color.FromArgb("#D1FAE5"),
        "pending"   => Color.FromArgb("#FEF3C7"),
        _           => Color.FromArgb("#EEF2F6"),
    };

    public Color BadgeTextColor => Status switch
    {
        "active"    => Color.FromArgb("#475569"),
        "completed" => Color.FromArgb("#065F46"),
        "pending"   => Color.FromArgb("#92400E"),
        _           => Color.FromArgb("#475569"),
    };
}
