using System.Text.Json.Serialization;

namespace TestHub.Models.Signature;

public sealed class SignatureDto
{
    [JsonPropertyName("signatureId")]
    public long SignatureId { get; set; }

    [JsonPropertyName("contractorId")]
    public long ContractorId { get; set; }

    [JsonPropertyName("signatureKey")]
    public string? SignatureKey { get; set; }

    [JsonPropertyName("signatureUrl")]
    public string? SignatureUrl { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
