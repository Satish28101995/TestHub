using System.Text.Json.Serialization;

namespace TestHub.Models.Contract;

public sealed class ContractCustomerDto
{
    [JsonPropertyName("customerId")]   public int CustomerId { get; set; }
    [JsonPropertyName("name")]         public string? Name { get; set; }
    [JsonPropertyName("emailAddress")] public string? EmailAddress { get; set; }
    [JsonPropertyName("phoneNumber")]  public string? PhoneNumber { get; set; }
    [JsonPropertyName("address")]      public string? Address { get; set; }
}
