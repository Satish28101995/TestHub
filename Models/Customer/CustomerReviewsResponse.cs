using System.Text.Json.Serialization;

namespace TestHub.Models.Customer;

/// <summary>
/// Response payload for <c>GET /v1/contractor/customer-reviews</c>.
/// </summary>
public sealed class CustomerReviewsResponse
{
    [JsonPropertyName("items")]      public List<CustomerReviewItem>? Items { get; set; }
    [JsonPropertyName("summary")]    public CustomerReviewsSummary? Summary { get; set; }
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    [JsonPropertyName("page")]       public int Page { get; set; }
    [JsonPropertyName("pageSize")]   public int PageSize { get; set; }
}
