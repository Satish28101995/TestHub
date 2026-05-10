using System.Text.Json.Serialization;

namespace TestHub.Models.Invoice;

/// <summary>
/// Aggregate counts/amounts returned by the invoices endpoint. Drives the
/// header tiles ("Total Invoices", "Total Paid") and the
/// "Outstanding Invoices" callout banner just above the list.
/// </summary>
public sealed class InvoicesSummary
{
    [JsonPropertyName("totalCount")]        public int TotalCount { get; set; }
    [JsonPropertyName("totalAmount")]       public decimal TotalAmount { get; set; }
    [JsonPropertyName("paidCount")]         public int PaidCount { get; set; }
    [JsonPropertyName("paidAmount")]        public decimal PaidAmount { get; set; }
    [JsonPropertyName("outstandingCount")]  public int OutstandingCount { get; set; }
    [JsonPropertyName("outstandingAmount")] public decimal OutstandingAmount { get; set; }
}
