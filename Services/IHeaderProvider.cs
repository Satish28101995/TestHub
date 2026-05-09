namespace TestHub.Services;

public interface IHeaderProvider
{
    /// <summary>
    /// Attaches the standard request headers (UtcOffsetInSecond, AppVersion,
    /// DeviceTypeId, LanguageCode) and, when the user is signed in, the
    /// AccessToken header plus an Authorization: Bearer header.
    /// </summary>
    void Attach(HttpRequestMessage request, bool requireAuth);
}
