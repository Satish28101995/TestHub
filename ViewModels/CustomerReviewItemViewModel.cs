using System.Globalization;
using TestHub.Models.Customer;

namespace TestHub.ViewModels;

/// <summary>
/// Per-row view model for a single customer review on the lookup
/// results list. Owns the formatting that the cell binds to so the
/// XAML stays declarative (no converters in the template).
/// </summary>
public sealed class CustomerReviewItemViewModel : BaseViewModel
{
    private readonly CustomerReviewItem _model;

    public CustomerReviewItemViewModel(CustomerReviewItem model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public long CustomerId => _model.CustomerId;

    public string CustomerName =>
        string.IsNullOrWhiteSpace(_model.CustomerName) ? "—" : _model.CustomerName!;

    public string Notes =>
        string.IsNullOrWhiteSpace(_model.Notes) ? string.Empty : _model.Notes!;

    /// <summary>
    /// Image URL for the avatar — only shown when the API returns a
    /// non-empty value. Otherwise the cell falls back to the initials
    /// chip.
    /// </summary>
    public string? ProfileImage =>
        string.IsNullOrWhiteSpace(_model.ProfileImage) ? null : _model.ProfileImage;

    public bool HasProfileImage => !string.IsNullOrWhiteSpace(_model.ProfileImage);
    public bool ShowInitials    => !HasProfileImage;

    /// <summary>
    /// Two-letter (or one-letter) initials chip rendered inside the
    /// avatar circle when no profile image is provided. Falls back to
    /// "?" if the customer has no name.
    /// </summary>
    public string Initials
    {
        get
        {
            var name = _model.CustomerName;
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            var parts = name.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                return "?";
            }

            if (parts.Length == 1)
            {
                return char.ToUpperInvariant(parts[0][0]).ToString();
            }

            return string.Concat(
                char.ToUpperInvariant(parts[0][0]),
                char.ToUpperInvariant(parts[^1][0])).ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Numeric rating shown next to the stars (e.g. "4.6"). One
    /// decimal place is enough resolution to read at a glance and
    /// keeps the chip a constant width.
    /// </summary>
    public string RatingDisplay =>
        _model.AvgRating.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>
    /// dd/MM/yyyy version of <c>ratingDate</c> used on the right of the
    /// rating row. Falls back to em-dash when the API didn't include
    /// the date.
    /// </summary>
    public string DateDisplay
    {
        get
        {
            if (_model.RatingDate is null)
            {
                return "—";
            }

            return _model.RatingDate.Value
                .ToLocalTime()
                .ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
    }

    public string ReviewCountLabel
    {
        get
        {
            var count = Math.Max(0, _model.ReviewCount);
            return count == 1 ? "1 Review" : $"{count} Reviews";
        }
    }

    // ------------------------------------------------------------------
    // Star fill flags — drive the 5 star icons in the row. Half-stars
    // round up to a filled star so the visual rating never reads low.
    // ------------------------------------------------------------------
    public bool Star1Filled => _model.AvgRating >= 0.5m;
    public bool Star2Filled => _model.AvgRating >= 1.5m;
    public bool Star3Filled => _model.AvgRating >= 2.5m;
    public bool Star4Filled => _model.AvgRating >= 3.5m;
    public bool Star5Filled => _model.AvgRating >= 4.5m;
}
