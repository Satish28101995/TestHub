using System.Text.Json.Serialization;

namespace TestHub.Models.Invoice;

/// <summary>
/// One row in the paginated <c>/v1/contractor/contracts/invoices</c>
/// response. Each invoice represents a contract milestone that has been
/// invoiced (or generated as a non-project, ad-hoc invoice).
/// </summary>
public sealed class InvoiceListItem
{
    [JsonPropertyName("contractMilestoneId")]
    public int ContractMilestoneId { get; set; }

    [JsonPropertyName("contractId")]
    public int ContractId { get; set; }

    [JsonPropertyName("customerId")]
    public int CustomerId { get; set; }

    [JsonPropertyName("milestoneName")]
    public string? MilestoneName { get; set; }

    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("invoiceGeneratedAtUtc")]
    public DateTime? InvoiceGeneratedAtUtc { get; set; }

    [JsonPropertyName("paidAmountDate")]
    public DateTime? PaidAmountDate { get; set; }

    [JsonPropertyName("paidVia")]
    public string? PaidVia { get; set; }

    [JsonPropertyName("isInvoiceGenerated")]
    public bool IsInvoiceGenerated { get; set; }

    [JsonPropertyName("isPaid")]
    public bool IsPaid { get; set; }

    /// <summary>
    /// Free-form server-side label such as "Paid", "Sent", "Pending".
    /// Used to drive the right-hand status pill on each invoice card.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
