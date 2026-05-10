namespace TestHub.Services;

/// <summary>
/// Returns a stable, per-install identifier used as the
/// <c>deviceToken</c> in auth requests (login + OTP verify).
/// </summary>
/// <remarks>
/// In production this should be replaced with the FCM/APNS push
/// token retrieved by the platform-specific notification handler.
/// Until that is wired up the implementation falls back to a GUID
/// persisted in <see cref="Microsoft.Maui.Storage.Preferences"/> so
/// the same value is sent on every call from a given install.
/// </remarks>
public interface IDeviceTokenProvider
{
    /// <summary>
    /// Returns the current device token. Implementations are
    /// expected to be idempotent and safe to call from any thread.
    /// </summary>
    Task<string> GetTokenAsync(CancellationToken ct = default);
}
