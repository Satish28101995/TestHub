using System.Text.Json.Serialization;

namespace TestHub.Models.Customer;

/// <summary>
/// One row in the paginated <c>/v1/contractor/customer-reviews</c>
/// response. Each row represents a customer with their aggregated
/// rating and the most recent review note.
/// </summary>
public sealed class CustomerReviewItem
{
    [JsonPropertyName("customerId")]   public long CustomerId { get; set; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; set; }
    [JsonPropertyName("profileImage")] public string? ProfileImage { get; set; }

    /// <summary>
    /// Average rating out of 5, rendered as a star count + numeric label
    /// (e.g. "4.6"). Sent as a decimal so half-stars are possible.
    /// </summary>
    [JsonPropertyName("avgRating")]    public decimal AvgRating { get; set; }

    /// <summary>
    /// Latest review body / note text shown under the customer name.
    /// </summary>
    [JsonPropertyName("notes")]        public string? Notes { get; set; }

    [JsonPropertyName("ratingDate")]   public DateTime? RatingDate { get; set; }

    [JsonPropertyName("reviewCount")]  public int ReviewCount { get; set; }
}
