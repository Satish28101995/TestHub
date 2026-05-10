using System.Text.Json.Serialization;

namespace TestHub.Models.Contract;

public sealed class ContractResponse
{
    [JsonPropertyName("contractId")]              public int ContractId { get; set; }
    [JsonPropertyName("quoteId")]                 public int QuoteId { get; set; }
    [JsonPropertyName("projectName")]             public string? ProjectName { get; set; }
    [JsonPropertyName("startDate")]               public DateTime StartDate { get; set; }
    [JsonPropertyName("endDate")]                 public DateTime EndDate { get; set; }
    [JsonPropertyName("totalContractAmount")]     public decimal TotalContractAmount { get; set; }
    [JsonPropertyName("termsAndConditions")]      public string? TermsAndConditions { get; set; }
    [JsonPropertyName("status")]                  public string? Status { get; set; }
    [JsonPropertyName("eSignedDateOnUtc")]        public DateTime? ESignedDateOnUtc { get; set; }
    [JsonPropertyName("isReviewGivenToCustomer")] public bool IsReviewGivenToCustomer { get; set; }
    [JsonPropertyName("customer")]                public ContractCustomerDto? Customer { get; set; }
    [JsonPropertyName("amounts")]                 public ContractAmountsDto? Amounts { get; set; }
    [JsonPropertyName("contractMilestones")]      public List<ContractMilestoneResponse>? ContractMilestones { get; set; }
}
