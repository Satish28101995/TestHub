using System.Text.Json.Serialization;

namespace TestHub.Models.Notification;

/// <summary>
/// One row in the paginated <c>/v1/contractor/notifications</c> response.
/// </summary>
public sealed class NotificationItem
{
    [JsonPropertyName("notificationId")]   public long NotificationId { get; set; }

    /// <summary>
    /// Server-side classification (1 = quote, 2 = contract, 3 = payment,
    /// 4 = message, 5 = milestone, etc.). The page itself doesn't switch
    /// on this value but it is preserved so deep-link routing can be
    /// added later without re-touching the model.
    /// </summary>
    [JsonPropertyName("notificationType")] public int NotificationType { get; set; }

    /// <summary>
    /// Id of the entity (quote / contract / invoice / message thread)
    /// that triggered the notification. Used by the row tap when we
    /// eventually deep-link into the correct screen.
    /// </summary>
    [JsonPropertyName("entityId")]         public long EntityId { get; set; }

    [JsonPropertyName("title")]            public string? Title { get; set; }
    [JsonPropertyName("message")]          public string? Message { get; set; }

    [JsonPropertyName("isRead")]           public bool IsRead { get; set; }
    [JsonPropertyName("isSent")]           public bool IsSent { get; set; }

    [JsonPropertyName("createdAtUtc")]     public DateTime? CreatedAtUtc { get; set; }
    [JsonPropertyName("readAtUtc")]        public DateTime? ReadAtUtc { get; set; }
}
