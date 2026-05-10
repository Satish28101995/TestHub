using System.Globalization;
using TestHub.Models.Quote;

namespace TestHub.ViewModels;

/// <summary>
/// Per-row view model for a single project (quote) on the Projects list.
/// Owns the formatting that the list cell binds to so the template stays
/// declarative and free of converters.
/// </summary>
public sealed class QuoteListItemViewModel : BaseViewModel
{
    private readonly QuoteListItem _model;

    public QuoteListItemViewModel(QuoteListItem model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public int QuoteId => _model.QuoteId;

    public string ProjectName =>
        string.IsNullOrWhiteSpace(_model.ProjectName) ? "Untitled Project" : _model.ProjectName!;

    public string CustomerName =>
        string.IsNullOrWhiteSpace(_model.CustomerName) ? "—" : _model.CustomerName!;

    /// <summary>
    /// Whole-dollar amount used by the "Amount" column on the card. Picks
    /// either <c>totalAmount</c> or <c>amount</c> depending on which the
    /// API populated.
    /// </summary>
    public string AmountDisplay
    {
        get
        {
            var value = _model.TotalAmount ?? _model.Amount ?? 0m;
            return string.Format(CultureInfo.InvariantCulture, "${0:0}", value);
        }
    }

    /// <summary>
    /// Day/Month/Year string matching the design (e.g. "01/05/2026").
    /// Falls back through the available date fields so we always show
    /// something useful even if the API only fills one of them.
    /// </summary>
    public string DateDisplay
    {
        get
        {
            var date = _model.StartDate ?? _model.CreatedDate ?? _model.CreatedAtUtc ?? _model.UpdatedAtUtc;
            return date is null ? "—" : date.Value.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
    }

    public string ProjectType =>
        string.IsNullOrWhiteSpace(_model.ProjectType) ? "—" : _model.ProjectType!.ToLowerInvariant();
}
