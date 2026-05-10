using System.Text.Json.Serialization;

namespace TestHub.Models.Invoice;

/// <summary>
/// Response payload for <c>GET /v1/contractor/contracts/invoices</c>. The
/// envelope mirrors the contracts list endpoint — paginated <c>items</c>
/// alongside a roll-up <c>summary</c>.
/// </summary>
public sealed class InvoicesListResponse
{
    [JsonPropertyName("items")]      public List<InvoiceListItem>? Items { get; set; }
    [JsonPropertyName("summary")]    public InvoicesSummary? Summary { get; set; }
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    [JsonPropertyName("page")]       public int Page { get; set; }
    [JsonPropertyName("pageSize")]   public int PageSize { get; set; }
}
