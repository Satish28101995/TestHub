using System.Text.Json.Serialization;

namespace TestHub.Models.Quote;

/// <summary>
/// Response payload for <c>GET /v1/contractor/quotes</c>.
/// Mirrors the pagination envelope used by the contracts list.
/// </summary>
public sealed class QuotesListResponse
{
    [JsonPropertyName("items")]      public List<QuoteListItem>? Items { get; set; }
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    [JsonPropertyName("page")]       public int Page { get; set; }
    [JsonPropertyName("pageSize")]   public int PageSize { get; set; }
}
