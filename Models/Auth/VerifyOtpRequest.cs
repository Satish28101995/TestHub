using System.Text.Json.Serialization;

namespace TestHub.Models.Auth;

/// <summary>
/// Request payload for <c>POST /v1/Account/Email/VerifyOtp</c>.
/// Sent after the user submits the 4-digit code from the OTP screen.
/// </summary>
public sealed class VerifyOtpRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("otp")]
    public string Otp { get; set; } = string.Empty;

    [JsonPropertyName("deviceToken")]
    public string DeviceToken { get; set; } = string.Empty;

    [JsonPropertyName("deviceType")]
    public int DeviceType { get; set; }

    [JsonPropertyName("userType")]
    public int UserType { get; set; } = (int)Models.Auth.UserType.Contractor;
}
