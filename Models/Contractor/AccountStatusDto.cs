using System.Text.Json.Serialization;

namespace TestHub.Models.Contractor;

public sealed class AccountStatusDto
{
    [JsonPropertyName("isGovernmentIdVerified")]
    public bool IsGovernmentIdVerified { get; set; }

    [JsonPropertyName("isBankAccountLinked")]
    public bool IsBankAccountLinked { get; set; }

    [JsonPropertyName("isESignatureAdded")]
    public bool IsESignatureAdded { get; set; }

    [JsonPropertyName("isTermsAndConditionsAdded")]
    public bool IsTermsAndConditionsAdded { get; set; }

    public bool AllComplete =>
        IsGovernmentIdVerified &&
        IsBankAccountLinked &&
        IsESignatureAdded &&
        IsTermsAndConditionsAdded;
}
