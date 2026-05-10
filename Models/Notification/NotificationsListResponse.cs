using System.Text.Json.Serialization;

namespace TestHub.Models.Notification;

/// <summary>
/// Response payload for <c>GET /v1/contractor/notifications</c>.
/// </summary>
/// <remarks>
/// Note that the notifications endpoint uses <c>pageNumber</c> (not
/// <c>page</c>) and exposes a <c>totalPages</c> field — the list is
/// keyed off that envelope so we can know when to stop paginating.
/// </remarks>
public sealed class NotificationsListResponse
{
    [JsonPropertyName("notifications")] public List<NotificationItem>? Notifications { get; set; }
    [JsonPropertyName("totalCount")]    public int TotalCount { get; set; }
    [JsonPropertyName("pageNumber")]    public int PageNumber { get; set; }
    [JsonPropertyName("pageSize")]      public int PageSize { get; set; }
    [JsonPropertyName("totalPages")]    public int TotalPages { get; set; }
}
