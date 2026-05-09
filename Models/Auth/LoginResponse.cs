using System.Text.Json.Serialization;

namespace TestHub.Models.Auth;

public sealed class LoginResponse
{
    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("profileImage")]
    public string? ProfileImage { get; set; }

    [JsonPropertyName("authorizationToken")]
    public string? AuthorizationToken { get; set; }

    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("userType")]
    public int UserType { get; set; }

    [JsonPropertyName("authProvider")]
    public string? AuthProvider { get; set; }

    [JsonPropertyName("isGovernmentIdVerified")]
    public bool IsGovernmentIdVerified { get; set; }

    [JsonPropertyName("isOnboardingComplete")]
    public bool IsOnboardingComplete { get; set; }
}
