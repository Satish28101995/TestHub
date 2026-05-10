using System.Text.Json.Serialization;

namespace TestHub.Models.Contract;

public sealed class ContractMilestoneRequest
{
    [JsonPropertyName("contractMilestoneId")] public int ContractMilestoneId { get; set; }
    [JsonPropertyName("milestoneName")]       public string? MilestoneName { get; set; }
    [JsonPropertyName("percentage")]          public decimal Percentage { get; set; }
    [JsonPropertyName("amount")]              public decimal Amount { get; set; }
}
