using System.Globalization;
using System.Net.Http.Headers;
using DeviceType = TestHub.Models.Auth.DeviceType;

namespace TestHub.Services;

public sealed class HeaderProvider : IHeaderProvider
{
    private readonly ISessionStore _session;

    public HeaderProvider(ISessionStore session)
    {
        _session = session;
    }

    public void Attach(HttpRequestMessage request, bool requireAuth)
    {
        // Headers required on every API call.
        Set(request, AppConfig.Headers.UtcOffsetInSecond,
            ((int)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds)
                .ToString(CultureInfo.InvariantCulture));

        Set(request, AppConfig.Headers.AppVersion, GetAppVersion());
        Set(request, AppConfig.Headers.DeviceTypeId,
            ((int)GetDeviceType()).ToString(CultureInfo.InvariantCulture));
        Set(request, AppConfig.Headers.LanguageCode, GetLanguageCode());

        // AccessToken header is sent on every authenticated call. Even on
        // anonymous calls we send an empty AccessToken header to satisfy
        // gateways that expect the key to be present.
        Set(request, AppConfig.Headers.AccessToken, _session.AccessToken ?? string.Empty);

        if (requireAuth && !string.IsNullOrEmpty(_session.AuthorizationToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", _session.AuthorizationToken);
        }
    }

    private static void Set(HttpRequestMessage request, string name, string value)
    {
        request.Headers.Remove(name);
        request.Headers.TryAddWithoutValidation(name, value);
    }

    private static string GetAppVersion()
    {
        try { return AppInfo.Current.VersionString; }
        catch { return "1.0"; }
    }

    private static string GetLanguageCode()
    {
        try { return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName; }
        catch { return "en"; }
    }

    public static DeviceType GetDeviceType()
    {
        try
        {
            var p = DeviceInfo.Current.Platform;
            if (p == DevicePlatform.Android)     return DeviceType.Android;
            if (p == DevicePlatform.iOS)         return DeviceType.iOS;
            if (p == DevicePlatform.WinUI)       return DeviceType.Windows;
            if (p == DevicePlatform.MacCatalyst) return DeviceType.MacCatalyst;
        }
        catch
        {
            // Fall through.
        }

        return DeviceType.Unknown;
    }
}
