using System.Text.Json.Serialization;

namespace TestHub.Models.Customer;

public sealed class CustomerDto
{
    [JsonPropertyName("customerId")]           public int CustomerId { get; set; }
    [JsonPropertyName("name")]                 public string? Name { get; set; }
    [JsonPropertyName("phoneNumber")]          public string? PhoneNumber { get; set; }
    [JsonPropertyName("emailAddress")]         public string? EmailAddress { get; set; }
    [JsonPropertyName("alternateName")]        public string? AlternateName { get; set; }
    [JsonPropertyName("alternatePhoneNumber")] public string? AlternatePhoneNumber { get; set; }
    [JsonPropertyName("notes")]                public string? Notes { get; set; }
    [JsonPropertyName("createdDate")]          public DateTime? CreatedDate { get; set; }
    [JsonPropertyName("addresses")]            public List<CustomerAddressDto>? Addresses { get; set; }
}
