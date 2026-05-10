using System.Text.Json.Serialization;

namespace TestHub.Models.Contract;

public sealed class ContractMilestoneResponse
{
    [JsonPropertyName("contractMilestoneId")]    public int ContractMilestoneId { get; set; }
    [JsonPropertyName("milestoneName")]          public string? MilestoneName { get; set; }
    [JsonPropertyName("percentage")]             public decimal Percentage { get; set; }
    [JsonPropertyName("amount")]                 public decimal Amount { get; set; }
    [JsonPropertyName("isPaid")]                 public bool IsPaid { get; set; }
    [JsonPropertyName("paidAmount")]             public decimal PaidAmount { get; set; }
    [JsonPropertyName("paidAtUtc")]              public DateTime? PaidAtUtc { get; set; }
    [JsonPropertyName("isInvoiceGenerated")]     public bool IsInvoiceGenerated { get; set; }
    [JsonPropertyName("invoiceGeneratedAtUtc")]  public DateTime? InvoiceGeneratedAtUtc { get; set; }
    [JsonPropertyName("paidVia")]                public string? PaidVia { get; set; }
}
