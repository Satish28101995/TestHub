using System.Text.Json.Serialization;

namespace TestHub.Models.Signature;

public sealed class UpdateSignatureRequest
{
    [JsonPropertyName("signatureUrl")]
    public string SignatureUrl { get; set; } = string.Empty;
}
