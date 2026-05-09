namespace TestHub.Services;

/// <summary>
/// Central place for API endpoints and header names.
/// </summary>
public static class AppConfig
{
    // TODO: Paste the real base URL here. The trailing slash matters.
    public const string BaseUrl = "https://odinapi-dev.24livehost.com/";

    public static class Endpoints
    {
        public const string Login            = "v1/Account/Login";
        public const string ForgetPassword   = "v1/Account/ForgetPassword";
        public const string GetSignature     = "v1/contractor/signature";
        public const string UpdateSignature  = "v1/contractor/update-signature";
        public const string GetTerms         = "v1/contractor/terms";
        public const string UpdateTerms      = "v1/contractor/update-terms";
    }

    public static class Headers
    {
        public const string UtcOffsetInSecond = "UtcOffsetInSecond";
        public const string AccessToken       = "AccessToken";
        public const string AppVersion        = "AppVersion";
        public const string DeviceTypeId      = "DeviceTypeId";
        public const string LanguageCode      = "LanguageCode";
        public const string Authorization     = "Authorization";
    }
}
