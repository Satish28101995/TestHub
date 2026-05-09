using System.Text.Json.Serialization;

namespace TestHub.Models.Auth;

public sealed class ForgetPasswordRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
