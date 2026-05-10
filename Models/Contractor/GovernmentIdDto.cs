using System.Text.Json.Serialization;

namespace TestHub.Models.Contractor;

/// <summary>
/// Response payload for <c>GET /v1/contractor/government-id</c>.
/// Used by the dashboard to decide whether to show the
/// "Action Required: Complete ID verification" banner.
/// </summary>
public sealed class GovernmentIdDto
{
    [JsonPropertyName("govIdId")]
    public int GovIdId { get; set; }

    [JsonPropertyName("contractorId")]
    public int ContractorId { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileUrl")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// True only when the API actually returned a usable government-id record
    /// (i.e. has a non-empty <see cref="FileUrl"/>). When the record is missing
    /// or its file URL is null/blank, the dashboard prompts the user to upload
    /// their ID via the "Action Required" banner.
    /// </summary>
    public bool HasUploadedId => !string.IsNullOrWhiteSpace(FileUrl);
}
