using System.Text.Json.Serialization;

namespace TestHub.Models.Auth;

/// <summary>
/// Request payload for <c>POST /v1/Account/ChangePassword</c>.
/// </summary>
public sealed class ChangePasswordRequest
{
    [JsonPropertyName("oldPassword")]
    public string OldPassword { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
