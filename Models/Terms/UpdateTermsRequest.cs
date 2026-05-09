using System.Text.Json.Serialization;

namespace TestHub.Models.Terms;

public sealed class UpdateTermsRequest
{
    [JsonPropertyName("termsAndConditions")]
    public string TermsAndConditions { get; set; } = string.Empty;
}
