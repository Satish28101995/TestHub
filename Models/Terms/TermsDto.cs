using System.Text.Json.Serialization;

namespace TestHub.Models.Terms;

public sealed class TermsDto
{
    [JsonPropertyName("contractorId")]
    public long ContractorId { get; set; }

    [JsonPropertyName("termsAndConditions")]
    public string? TermsAndConditions { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
