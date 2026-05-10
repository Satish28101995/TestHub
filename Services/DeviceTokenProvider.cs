using Microsoft.Maui.Storage;

namespace TestHub.Services;

/// <summary>
/// Default implementation that backs the device token by a GUID kept
/// in <see cref="Preferences"/>. The same value is returned on every
/// subsequent call, including across app restarts, so the server can
/// reliably identify a single install.
/// </summary>
public sealed class DeviceTokenProvider : IDeviceTokenProvider
{
    private const string PreferenceKey = "device_token";

    public Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        try
        {
            var existing = Preferences.Default.Get(PreferenceKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return Task.FromResult(existing);
            }

            var token = Guid.NewGuid().ToString("N");
            Preferences.Default.Set(PreferenceKey, token);
            return Task.FromResult(token);
        }
        catch
        {
            // Preferences can throw on unsupported platforms — fall back to
            // a per-process GUID rather than crashing the auth flow.
            return Task.FromResult(Guid.NewGuid().ToString("N"));
        }
    }
}
