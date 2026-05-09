using System.Text.Json.Serialization;

namespace TestHub.Models.Auth;

public sealed class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("deviceToken")]
    public string DeviceToken { get; set; } = string.Empty;

    [JsonPropertyName("deviceType")]
    public int DeviceType { get; set; }

    [JsonPropertyName("userType")]
    public int UserType { get; set; } = (int)Models.Auth.UserType.Contractor;
}
