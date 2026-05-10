using System.Globalization;
using TestHub.Models.Notification;

namespace TestHub.ViewModels;

/// <summary>
/// Per-row view model for the notifications list. Owns the
/// time-ago formatting and the unread accent that the cell binds to,
/// keeping the XAML template free of converters.
/// </summary>
public sealed class NotificationItemViewModel : BaseViewModel
{
    private readonly NotificationItem _model;
    private bool _isRead;

    public NotificationItemViewModel(NotificationItem model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _isRead = model.IsRead;
    }

    public long NotificationId      => _model.NotificationId;
    public int  NotificationType    => _model.NotificationType;
    public long EntityId            => _model.EntityId;

    public string Title   => string.IsNullOrWhiteSpace(_model.Title)   ? "Notification" : _model.Title!;
    public string Message => string.IsNullOrWhiteSpace(_model.Message) ? string.Empty   : _model.Message!;

    /// <summary>
    /// True until the user opens the row. Drives the gold left-edge
    /// accent stripe and the small gold dot bullet next to the title.
    /// Once toggled false the title also drops back to a regular weight
    /// so already-seen rows look obviously different.
    /// </summary>
    public bool IsUnread
    {
        get => !_isRead;
        private set
        {
            // Stored as IsRead so we can toggle it via MarkAsRead.
            var newRead = !value;
            if (_isRead == newRead) return;
            _isRead = newRead;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TitleFontAttributes));
            OnPropertyChanged(nameof(TitleColor));
            OnPropertyChanged(nameof(AccentColor));
        }
    }

    /// <summary>
    /// Called by the list VM after a successful POST to
    /// <c>/v1/contractor/notifications/{id}/read</c>. Idempotent.
    /// </summary>
    public void MarkAsRead() => IsUnread = false;

    /// <summary>
    /// Title weight changes once the row has been opened — unread rows
    /// stay bold; read rows dim into a regular weight so the eye can
    /// scan to the unread items.
    /// </summary>
    public FontAttributes TitleFontAttributes => IsUnread ? FontAttributes.Bold : FontAttributes.None;
    public Color           TitleColor          => IsUnread ? Color.FromArgb("#0F172A") : Color.FromArgb("#475569");

    /// <summary>
    /// Drives the thin left-edge accent stripe on the card. Electric indigo
    /// (BrandAccent #6366F1 from Resources/Styles/Colors.xaml) for unread
    /// rows, transparent for read rows so the layout still reserves the
    /// same horizontal space and rows don't shift when the user opens one.
    /// </summary>
    public Color AccentColor => IsUnread ? Color.FromArgb("#6366F1") : Colors.Transparent;

    /// <summary>
    /// Pretty "x ago" stamp computed from <c>createdAtUtc</c>. Uses
    /// progressively coarser units (sec → min → hour → day) and then
    /// falls back to date / month / year for older notifications, all
    /// localised to the device's clock so "9:14 AM" matches what the
    /// user expects.
    /// </summary>
    public string TimeAgoDisplay => FormatTimeAgo(_model.CreatedAtUtc);

    /// <summary>
    /// Public, unit-testable helper that the page also exposes so other
    /// parts of the app (toast, dashboard bell badge) can format the
    /// same way.
    /// </summary>
    public static string FormatTimeAgo(DateTime? createdAtUtc)
    {
        if (createdAtUtc is null)
        {
            return string.Empty;
        }

        // Treat the inbound timestamp as UTC. Some servers omit the
        // 'Z' suffix so DateTime.Kind comes back Unspecified; coerce
        // it so the diff math is always anchored in UTC.
        var createdUtc = createdAtUtc.Value.Kind == DateTimeKind.Utc
            ? createdAtUtc.Value
            : DateTime.SpecifyKind(createdAtUtc.Value, DateTimeKind.Utc);

        var nowUtc = DateTime.UtcNow;
        var delta  = nowUtc - createdUtc;

        // Future timestamps (clock skew on the server) — show "now".
        if (delta.TotalSeconds < 0)
        {
            return "now";
        }

        if (delta.TotalSeconds < 5)
        {
            return "now";
        }

        if (delta.TotalSeconds < 60)
        {
            var s = (int)Math.Floor(delta.TotalSeconds);
            return $"{s}s ago";
        }

        if (delta.TotalMinutes < 60)
        {
            var m = (int)Math.Floor(delta.TotalMinutes);
            return $"{m}m ago";
        }

        if (delta.TotalHours < 24)
        {
            var h = (int)Math.Floor(delta.TotalHours);
            return $"{h}h ago";
        }

        // Same calendar day in local time — show the wall clock.
        var localCreated = createdUtc.ToLocalTime();
        var localNow     = DateTime.Now;
        if (localCreated.Date == localNow.Date)
        {
            return localCreated.ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        if (delta.TotalDays < 7)
        {
            var d = (int)Math.Floor(delta.TotalDays);
            return d == 1 ? "1d ago" : $"{d}d ago";
        }

        if (localCreated.Year == localNow.Year)
        {
            // Within the same year — show "12 May" style date.
            return localCreated.ToString("d MMM", CultureInfo.InvariantCulture);
        }

        // Anything older falls back to a full date.
        return localCreated.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
    }
}
