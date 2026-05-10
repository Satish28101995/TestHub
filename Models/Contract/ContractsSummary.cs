using System.Text.Json.Serialization;

namespace TestHub.Models.Contract;

public sealed class ContractsSummary
{
    [JsonPropertyName("active")]    public int Active { get; set; }
    [JsonPropertyName("pending")]   public int Pending { get; set; }
    [JsonPropertyName("completed")] public int Completed { get; set; }
}
