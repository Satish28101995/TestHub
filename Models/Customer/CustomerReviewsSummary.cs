using System.Text.Json.Serialization;

namespace TestHub.Models.Customer;

/// <summary>
/// Aggregate summary returned alongside the paginated list. Used for
/// the "X Reviews" badge and the average-rating star line on the
/// results header.
/// </summary>
public sealed class CustomerReviewsSummary
{
    [JsonPropertyName("totalCount")]   public int TotalCount { get; set; }
    [JsonPropertyName("totalAverage")] public decimal TotalAverage { get; set; }
}
