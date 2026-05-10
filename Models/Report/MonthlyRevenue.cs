using System.Text.Json.Serialization;

namespace TestHub.Models.Report;

/// <summary>
/// One bar on the Revenue Trend chart — a single month with its display
/// label and the revenue amount earned that month.
/// </summary>
public sealed class MonthlyRevenue
{
    /// <summary>1-based month index (1 = January … 12 = December).</summary>
    [JsonPropertyName("month")]
    public int Month { get; set; }

    /// <summary>Human-friendly label e.g. "Jan", "Feb"… provided by the API.</summary>
    [JsonPropertyName("monthLabel")]
    public string? MonthLabel { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}
