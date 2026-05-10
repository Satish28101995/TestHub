using System.Text.Json.Serialization;

namespace TestHub.Models.Contract;

public sealed class ContractsListResponse
{
    [JsonPropertyName("items")]      public List<ContractListItem>? Items { get; set; }
    [JsonPropertyName("summary")]    public ContractsSummary? Summary { get; set; }
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    [JsonPropertyName("page")]       public int Page { get; set; }
    [JsonPropertyName("pageSize")]   public int PageSize { get; set; }
}
