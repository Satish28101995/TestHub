using System.Globalization;
using TestHub.Models.Contract;

namespace TestHub.ViewModels;

/// <summary>
/// Display wrapper around <see cref="ContractListItem"/>. Pre-formats every
/// string the contracts list cell needs (date, amount, progress label, status
/// pill colors) so the XAML stays free of converters.
/// </summary>
public sealed class ContractListItemViewModel
{
    private static readonly CultureInfo s_money = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo s_invariant = CultureInfo.InvariantCulture;

    private readonly ContractListItem _src;

    public ContractListItemViewModel(ContractListItem src)
    {
        _src = src;
    }

    public int ContractId => _src.ContractId;

    public string ProjectName => string.IsNullOrWhiteSpace(_src.ProjectName)
        ? "Untitled project"
        : _src.ProjectName!;

    public string CustomerName => _src.CustomerName ?? string.Empty;

    public string DateDisplay => _src.StartDate == default
        ? string.Empty
        : _src.StartDate.ToString("MMM d, yyyy", s_invariant);

    public string AmountDisplay => _src.TotalContractAmount.ToString("C0", s_money);

    public string ProgressLabel =>
        $"{_src.CompletedMilestones}/{_src.TotalMilestones}";

    public double ProgressFraction => _src.TotalMilestones > 0
        ? Math.Clamp((double)_src.CompletedMilestones / _src.TotalMilestones, 0d, 1d)
        : 0d;

    public bool HasMilestones => _src.TotalMilestones > 0;

    // ------------ Status helpers ------------
    private string NormalizedStatus =>
        (_src.Status ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();

    public bool IsPendingStatus =>
        NormalizedStatus is "pending" or "pendingsignature";

    public bool IsCompletedStatus =>
        NormalizedStatus is "completed" or "complete" or "done";

    /// <summary>True for both "active" and "completed" — anything that's not pending.</summary>
    public bool IsHealthyStatus => !IsPendingStatus;

    public string StatusBadgeLabel => NormalizedStatus switch
    {
        "completed" or "complete" or "done"          => "Completed",
        "pendingsignature" or "pending"              => "Pending Signature",
        "inprogress" or "active"                     => "In Progress",
        _                                            => string.IsNullOrWhiteSpace(_src.Status) ? "—" : _src.Status!,
    };

    public Color StatusBadgeBackground => NormalizedStatus switch
    {
        "completed" or "complete" or "done"          => Color.FromArgb("#D1FAE5"),
        "pendingsignature" or "pending"              => Color.FromArgb("#FEF3C7"),
        "inprogress" or "active"                     => Color.FromArgb("#F1F5F9"),
        _                                            => Color.FromArgb("#F1F5F9"),
    };

    public Color StatusBadgeText => NormalizedStatus switch
    {
        "completed" or "complete" or "done"          => Color.FromArgb("#10B981"),
        "pendingsignature" or "pending"              => Color.FromArgb("#92400E"),
        "inprogress" or "active"                     => Color.FromArgb("#475569"),
        _                                            => Color.FromArgb("#475569"),
    };
}
