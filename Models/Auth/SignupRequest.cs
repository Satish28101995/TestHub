using System.Text.Json.Serialization;

namespace TestHub.Models.Auth;

/// <summary>
/// Wire payload for <c>POST /v1/Account/SignUp</c>. Property names map
/// 1:1 to the server contract via <see cref="JsonPropertyNameAttribute"/>;
/// keep them in sync if the API spec changes.
///
/// NOTE: The server-side property is intentionally spelled
/// <c>termsAndCondtion</c> (missing an "i") — that typo is in the
/// published OpenAPI contract, so the JSON name preserves it verbatim.
/// The C# property uses the corrected spelling for readability.
/// </summary>
public sealed class SignupRequest
{
    // ---------- Address ----------

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("streetNumber")]
    public string StreetNumber { get; set; } = string.Empty;

    [JsonPropertyName("streetName")]
    public string StreetName { get; set; } = string.Empty;

    [JsonPropertyName("suburb")]
    public string Suburb { get; set; } = string.Empty;

    [JsonPropertyName("postcode")]
    public string Postcode { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    // ---------- Contact / identity ----------

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("contactName")]
    public string ContactName { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    // ---------- Business identifiers ----------

    [JsonPropertyName("abnNumber")]
    public string AbnNumber { get; set; } = string.Empty;

    [JsonPropertyName("licenceNumber")]
    public string LicenceNumber { get; set; } = string.Empty;

    [JsonPropertyName("website")]
    public string Website { get; set; } = string.Empty;

    // ---------- Uploads (server-hosted URLs) ----------

    [JsonPropertyName("logoUrl")]
    public string LogoUrl { get; set; } = string.Empty;

    [JsonPropertyName("governmentIdFileUrl")]
    public string GovernmentIdFileUrl { get; set; } = string.Empty;

    // ---------- Credentials + consent ----------

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Terms & conditions acceptance. JSON name preserves the
    /// upstream spec's "Condtion" typo on purpose — see class summary.
    /// </summary>
    [JsonPropertyName("termsAndCondtion")]
    public bool TermsAndCondition { get; set; }

    // ---------- Discriminators ----------

    [JsonPropertyName("userType")]
    public int UserType { get; set; } = (int)Models.Auth.UserType.Contractor;

    [JsonPropertyName("deviceType")]
    public int DeviceType { get; set; }
}
