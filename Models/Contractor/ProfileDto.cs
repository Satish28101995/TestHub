using System.Text.Json.Serialization;

namespace TestHub.Models.Contractor;

public sealed class ProfileDto
{
    [JsonPropertyName("location")]      public string? Location { get; set; }
    [JsonPropertyName("streetNumber")]  public string? StreetNumber { get; set; }
    [JsonPropertyName("streetName")]    public string? StreetName { get; set; }
    [JsonPropertyName("suburb")]        public string? Suburb { get; set; }
    [JsonPropertyName("postcode")]      public string? Postcode { get; set; }
    [JsonPropertyName("latitude")]      public double Latitude { get; set; }
    [JsonPropertyName("longitude")]     public double Longitude { get; set; }
    [JsonPropertyName("email")]         public string? Email { get; set; }
    [JsonPropertyName("firstName")]     public string? FirstName { get; set; }
    [JsonPropertyName("lastName")]      public string? LastName { get; set; }
    [JsonPropertyName("companyName")]   public string? CompanyName { get; set; }
    [JsonPropertyName("contactName")]   public string? ContactName { get; set; }
    [JsonPropertyName("phoneNumber")]   public string? PhoneNumber { get; set; }
    [JsonPropertyName("abnNumber")]     public string? AbnNumber { get; set; }
    [JsonPropertyName("licenceNumber")] public string? LicenceNumber { get; set; }
    [JsonPropertyName("website")]       public string? Website { get; set; }
    [JsonPropertyName("profileImage")]  public string? ProfileImage { get; set; }
    [JsonPropertyName("governmentIdFileUrl")] public string? GovernmentIdFileUrl { get; set; }

    // Note: server uses the misspelt "termsAndCondtion" key — preserved here.
    [JsonPropertyName("termsAndCondtion")] public bool TermsAndConditions { get; set; }
}
