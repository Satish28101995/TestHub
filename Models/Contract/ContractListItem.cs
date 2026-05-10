using System.Text.Json.Serialization;

namespace TestHub.Models.Contract;

public sealed class ContractListItem
{
    [JsonPropertyName("contractId")]          public int ContractId { get; set; }
    [JsonPropertyName("quoteId")]             public int? QuoteId { get; set; }
    [JsonPropertyName("projectName")]         public string? ProjectName { get; set; }
    [JsonPropertyName("customerName")]        public string? CustomerName { get; set; }
    [JsonPropertyName("startDate")]           public DateTime StartDate { get; set; }
    [JsonPropertyName("totalContractAmount")] public decimal TotalContractAmount { get; set; }
    [JsonPropertyName("status")]              public string? Status { get; set; }
    [JsonPropertyName("totalMilestones")]     public int TotalMilestones { get; set; }
    [JsonPropertyName("completedMilestones")] public int CompletedMilestones { get; set; }
    [JsonPropertyName("updatedAtUtc")]        public DateTime UpdatedAtUtc { get; set; }
}
