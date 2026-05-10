using System.Text.Json.Serialization;

namespace TestHub.Models.Customer;

public sealed class CustomerAddressDto
{
    [JsonPropertyName("addressId")]    public int AddressId { get; set; }
    [JsonPropertyName("label")]        public string? Label { get; set; }
    [JsonPropertyName("location")]     public string? Location { get; set; }
    [JsonPropertyName("streetNumber")] public string? StreetNumber { get; set; }
    [JsonPropertyName("streetName")]   public string? StreetName { get; set; }
    [JsonPropertyName("suburb")]       public string? Suburb { get; set; }
    [JsonPropertyName("postcode")]     public string? Postcode { get; set; }
    [JsonPropertyName("latitude")]     public double Latitude { get; set; }
    [JsonPropertyName("longitude")]    public double Longitude { get; set; }
    [JsonPropertyName("isPrimary")]    public bool IsPrimary { get; set; }

    /// <summary>
    /// Returns a human-readable single-line address composed of the parts
    /// the server returns. Falls back to the <see cref="Location"/> string
    /// when individual fields are blank.
    /// </summary>
    public string ToDisplay()
    {
        var parts = new[] { StreetNumber, StreetName, Suburb, Postcode }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (parts.Length > 0)
        {
            return string.Join(" ", parts!).Trim();
        }

        return Location ?? string.Empty;
    }
}
