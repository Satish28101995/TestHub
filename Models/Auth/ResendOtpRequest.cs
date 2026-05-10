using System.Text.Json.Serialization;

namespace TestHub.Models.Auth;

/// <summary>
/// Request payload for <c>POST /v1/Account/Email/Verify/Resend</c>.
/// </summary>
public sealed class ResendOtpRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
