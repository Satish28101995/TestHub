using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Messaging;
using TestHub.Services;
using AndroidApp = Android.App.Application;

namespace TestHub.Platforms.Android.Services;

/// <summary>
/// Android-side bridge to Firebase Cloud Messaging.
///
/// Two responsibilities:
/// <list type="bullet">
///   <item>
///     <description>
///       <c>OnNewToken</c> — fired by FCM whenever the registration token
///       is generated for the first time, refreshed, or rotated. We push
///       the new value into <see cref="DeviceTokenProvider"/> so the very
///       next login / OTP-verify request carries the correct token without
///       waiting for an explicit refresh from the UI layer.
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>OnMessageReceived</c> — fired when a push arrives while the
///       app is in the foreground (background pushes are rendered by FCM
///       directly when they include a <c>notification</c> payload). We
///       turn data-only payloads into a system notification so the user
///       always sees an alert in the tray.
///     </description>
///   </item>
/// </list>
///
/// Registered in <c>AndroidManifest.xml</c> with the
/// <c>com.google.firebase.MESSAGING_EVENT</c> intent filter so FCM can
/// dispatch tokens and messages here.
/// </summary>
[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public sealed class MyFirebaseMessagingService : FirebaseMessagingService
{
    // The channel ID is a stable developer identifier and intentionally
    // stays "testhub_default" — changing it on an upgrade would orphan
    // the channel that already exists on users' devices and the new
    // channel name (below) would silently fail to apply. Only the
    // user-visible channel NAME is rebranded to match ApplicationTitle.
    private const string DefaultChannelId = "testhub_default";
    private const string DefaultChannelName = "DemoApp Notifications";

    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);

        // Push the freshly minted token into the shared cache so the next
        // call to IDeviceTokenProvider.GetTokenAsync() returns this value
        // immediately — no extra FCM round-trip required.
        DeviceTokenProvider.StoreToken(token);
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        try
        {
            // Some pushes ship a notification payload (auto-displayed by
            // the OS when the app is backgrounded). When we get one in
            // the foreground we surface it ourselves so the user still
            // sees a visible alert.
            var title = message.GetNotification()?.Title
                        ?? (message.Data.TryGetValue("title", out var t) ? t : "DemoApp");
            var body  = message.GetNotification()?.Body
                        ?? (message.Data.TryGetValue("message", out var m) ? m : string.Empty);

            ShowNotification(title, body);
        }
        catch
        {
            // Silently swallow — a malformed payload should never crash
            // the messaging service (Android would mark the app as
            // unstable for FCM and back off delivery).
        }
    }

    private void ShowNotification(string title, string body)
    {
        var context = AndroidApp.Context;

        EnsureChannel(context);

        var builder = new NotificationCompat.Builder(context, DefaultChannelId)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetAutoCancel(true)
            .SetPriority(NotificationCompat.PriorityDefault)!;

        var manager = NotificationManagerCompat.From(context);
        manager.Notify(NotificationId(), builder.Build()!);
    }

    private static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var manager = (NotificationManager?)context.GetSystemService(NotificationService);
        if (manager is null)
        {
            return;
        }

        if (manager.GetNotificationChannel(DefaultChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(
            DefaultChannelId,
            DefaultChannelName,
            NotificationImportance.Default);

        manager.CreateNotificationChannel(channel);
    }

    /// <summary>
    /// Notifications need a unique id per call so multiple pushes don't
    /// overwrite each other in the system tray. We use the lower 31 bits
    /// of <see cref="DateTimeOffset.UtcNow"/> milliseconds — large enough
    /// to avoid collisions in any realistic session.
    /// </summary>
    private static int NotificationId()
        => (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() & 0x7FFFFFFF);
}
