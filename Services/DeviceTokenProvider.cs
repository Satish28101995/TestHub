using Microsoft.Maui.Storage;

#if ANDROID
// We deliberately avoid `using Android.Gms.Tasks;` because that namespace
// also exposes a CancellationToken type which would collide with
// System.Threading.CancellationToken on this file. Aliasing the two
// types we actually need keeps the rest of the file unambiguous.
using GmsTask = Android.Gms.Tasks.Task;
using IOnCompleteListener = Android.Gms.Tasks.IOnCompleteListener;
using Firebase.Messaging;
#elif IOS
using Firebase.CloudMessaging;
#endif

namespace TestHub.Services;

/// <summary>
/// Default implementation of <see cref="IDeviceTokenProvider"/>.
///
/// The provider is strictly an FCM-token source — it never invents a
/// synthetic identifier. An empty string is returned when Firebase
/// hasn't yet handed us a registration token (most commonly because
/// <c>google-services.json</c> / <c>GoogleService-Info.plist</c> hasn't
/// been added to the project; see <c>Platforms/Android/FCM_SETUP.md</c>
/// or <c>Platforms/iOS/FCM_SETUP.md</c>).
///
/// Behaviour by platform:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Android</b>: returns the Firebase Cloud Messaging registration
///       token via <c>FirebaseMessaging.Instance.GetToken()</c>. The
///       token is persisted to <see cref="Preferences"/> so subsequent
///       calls resolve from the cache without a Firebase round-trip.
///       <c>MyFirebaseMessagingService.OnNewToken</c> writes the same
///       key whenever the FCM SDK rotates the token, so the cache
///       always reflects the latest value.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>iOS / Mac Catalyst</b>: returns the FCM registration token
///       sourced from <c>Messaging.SharedInstance.FcmToken</c>.
///       <c>AppDelegate.DidReceiveRegistrationToken</c> feeds rotated
///       tokens through <see cref="StoreToken"/> into the same shared
///       cache.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Other platforms</b> (Windows): no FCM SDK available — the
///       provider returns an empty string. Push notifications can be
///       wired up by adding a Windows-specific FCM / WNS source later.
///     </description>
///   </item>
/// </list>
/// </summary>
public sealed class DeviceTokenProvider : IDeviceTokenProvider
{
    /// <summary>
    /// Key that holds the most recent FCM registration token. Shared
    /// between this provider, the Android
    /// <c>FirebaseMessagingService.OnNewToken</c> hook and the iOS
    /// <c>AppDelegate.DidReceiveRegistrationToken</c> hook so any of
    /// the three writers keeps the cache in sync.
    /// </summary>
    public const string PreferenceKey = "device_token";

    /// <summary>
    /// Hot in-memory cache that lets us answer <see cref="GetTokenAsync"/>
    /// synchronously after the first successful read. Updated by both
    /// <see cref="GetTokenAsync"/> and the FCM service's OnNewToken.
    /// </summary>
    private static volatile string? _cachedToken;

    public async Task<string> GetTokenAsync(System.Threading.CancellationToken ct = default)
    {
        if (LooksLikeFcmToken(_cachedToken))
        {
            return _cachedToken!;
        }

        var stored = SafeReadPreference();
        if (LooksLikeFcmToken(stored))
        {
            _cachedToken = stored;
            return stored;
        }

        // A non-FCM value sitting in the cache is a leftover from an older
        // build that wrote a GUID fallback. Clear it so the next caller
        // doesn't accidentally surface it as a deviceToken.
        if (!string.IsNullOrEmpty(stored))
        {
            ClearPersistedToken();
        }

#if ANDROID || IOS
        var fcm = await TryGetFcmTokenAsync(ct).ConfigureAwait(false);
        if (LooksLikeFcmToken(fcm))
        {
            PersistToken(fcm!);
            return fcm!;
        }
#else
        await Task.CompletedTask.ConfigureAwait(false);
#endif

        // Firebase isn't configured (or hasn't returned a token yet).
        // We deliberately do NOT invent a GUID here — push notifications
        // require a real FCM registration token, so an empty value is
        // the honest answer until the platform delegate hands us one
        // via StoreToken.
        return string.Empty;
    }

    /// <summary>
    /// Distinguishes a real FCM registration token from leftover values
    /// (empty string or the legacy 32-char GUID fallback). FCM tokens
    /// are always 100+ characters and contain a colon separator — e.g.
    /// <c>cZ6q-rQXTnSlDx6cVxxxxx:APA91bF...</c>. Anything shorter or
    /// without a colon is treated as not-a-token.
    /// </summary>
    private static bool LooksLikeFcmToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value!.Length >= 64 && value.Contains(':');
    }

    private static void ClearPersistedToken()
    {
        _cachedToken = null;
        try
        {
            Preferences.Default.Remove(PreferenceKey);
        }
        catch
        {
            // Best-effort; the in-memory cache is now empty either way.
        }
    }

    /// <summary>
    /// Updates the cache + persisted value. Called from
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Android: <c>MyFirebaseMessagingService.OnNewToken</c>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       iOS: <c>AppDelegate.DidReceiveRegistrationToken</c>
    ///     </description>
    ///   </item>
    /// </list>
    /// so a freshly rotated FCM token is immediately visible to the next
    /// login / OTP-verify request.
    /// </summary>
    public static void StoreToken(string token)
    {
        if (!LooksLikeFcmToken(token))
        {
            return;
        }

        PersistToken(token);
    }

    private static void PersistToken(string token)
    {
        _cachedToken = token;
        try
        {
            Preferences.Default.Set(PreferenceKey, token);
        }
        catch
        {
            // Preferences may be unavailable on certain platforms (e.g.
            // unit-test contexts) — a missed write only costs us one extra
            // FCM round-trip on the next call, which is acceptable.
        }
    }

    private static string SafeReadPreference()
    {
        try
        {
            return Preferences.Default.Get(PreferenceKey, string.Empty);
        }
        catch
        {
            return string.Empty;
        }
    }

#if ANDROID
    /// <summary>
    /// Asks Firebase for the current FCM registration token. Wrapped in
    /// a try/catch so a missing <c>google-services.json</c> or a stale
    /// Play Services install never throws into the auth pipeline.
    /// </summary>
    private static async Task<string?> TryGetFcmTokenAsync(System.Threading.CancellationToken ct)
    {
        try
        {
            var task = FirebaseMessaging.Instance.GetToken();
            return await AwaitJavaTaskAsync(task, ct).ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            return null;
        }
    }

    /// <summary>
    /// Bridges <see cref="GmsTask"/> (Java's awaitable) to a .NET
    /// <see cref="System.Threading.Tasks.Task{TResult}"/> via an
    /// <see cref="IOnCompleteListener"/>. Cancellation honours the
    /// caller's <paramref name="ct"/>.
    /// </summary>
    private static Task<string?> AwaitJavaTaskAsync(GmsTask task, System.Threading.CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (ct.CanBeCanceled)
        {
            ct.Register(() => tcs.TrySetCanceled(ct));
        }

        task.AddOnCompleteListener(new TokenCompleteListener(tcs));
        return tcs.Task;
    }

    private sealed class TokenCompleteListener : Java.Lang.Object, IOnCompleteListener
    {
        private readonly TaskCompletionSource<string?> _tcs;

        public TokenCompleteListener(TaskCompletionSource<string?> tcs)
        {
            _tcs = tcs;
        }

        public void OnComplete(GmsTask task)
        {
            try
            {
                if (!task.IsSuccessful)
                {
                    _tcs.TrySetResult(null);
                    return;
                }

                var result = task.Result?.ToString();
                _tcs.TrySetResult(string.IsNullOrEmpty(result) ? null : result);
            }
            catch
            {
                _tcs.TrySetResult(null);
            }
        }
    }
#endif

#if IOS
    /// <summary>
    /// Reads the cached FCM registration token from the Firebase iOS
    /// SDK. The token is populated by the Firebase SDK shortly after
    /// <c>App.Configure()</c> and the APNs token are wired up — the
    /// authoritative way we receive it is via
    /// <c>AppDelegate.DidReceiveRegistrationToken</c>, which calls
    /// <see cref="StoreToken"/> directly. This method exists so that a
    /// caller hitting <see cref="GetTokenAsync"/> before the delegate
    /// callback fires still gets the token if the SDK has it ready.
    ///
    /// Wrapped in a try/catch so a missing
    /// <c>GoogleService-Info.plist</c> or a Firebase-not-yet-configured
    /// state never throws into the auth pipeline — the caller will see
    /// an empty deviceToken until <see cref="StoreToken"/> is invoked
    /// from the delegate callback with a real registration token.
    /// </summary>
    private static Task<string?> TryGetFcmTokenAsync(System.Threading.CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            var token = Messaging.SharedInstance?.FcmToken;
            return Task.FromResult<string?>(string.IsNullOrEmpty(token) ? null : token);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }
#endif
}
