using System.Text.Json.Serialization;

namespace TestHub.Models.Contract;

public sealed class ContractAmountsDto
{
    [JsonPropertyName("totalContractAmount")]   public decimal TotalContractAmount { get; set; }
    [JsonPropertyName("outstandingAmount")]     public decimal OutstandingAmount { get; set; }
    [JsonPropertyName("paidAmount")]            public decimal PaidAmount { get; set; }
    [JsonPropertyName("variationsAmount")]      public decimal VariationsAmount { get; set; }
    [JsonPropertyName("originalBudgetAmount")]  public decimal OriginalBudgetAmount { get; set; }
}
