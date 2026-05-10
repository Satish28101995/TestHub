using System.Text.Json.Serialization;

namespace TestHub.Models.Quote;

/// <summary>
/// One row in the paginated <c>/v1/contractor/quotes</c> response. Field
/// names follow the same envelope conventions used by the contracts list
/// (camelCase keys returned by the API).
/// </summary>
public sealed class QuoteListItem
{
    [JsonPropertyName("quoteId")]       public int QuoteId { get; set; }
    [JsonPropertyName("projectName")]   public string? ProjectName { get; set; }
    [JsonPropertyName("customerName")]  public string? CustomerName { get; set; }

    /// <summary>
    /// Total amount for the quote / project. Some backends return this as
    /// <c>totalAmount</c>, others as <c>amount</c>; we accept both so the
    /// list still renders if the field name varies.
    /// </summary>
    [JsonPropertyName("totalAmount")]   public decimal? TotalAmount { get; set; }
    [JsonPropertyName("amount")]        public decimal? Amount { get; set; }

    [JsonPropertyName("startDate")]     public DateTime? StartDate { get; set; }
    [JsonPropertyName("createdDate")]   public DateTime? CreatedDate { get; set; }
    [JsonPropertyName("createdAtUtc")]  public DateTime? CreatedAtUtc { get; set; }
    [JsonPropertyName("updatedAtUtc")]  public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Free-form project type label rendered in the "Type" column on the
    /// project card (e.g. "standard", "random").
    /// </summary>
    [JsonPropertyName("projectType")]   public string? ProjectType { get; set; }

    [JsonPropertyName("status")]        public string? Status { get; set; }
}
