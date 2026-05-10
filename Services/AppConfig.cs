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
        public const string Logout           = "v1/Account/Logout";
        public const string ForgetPassword   = "v1/Account/ForgetPassword";
        public const string ChangePassword   = "v1/Account/ChangePassword";
        public const string Profile          = "v1/Profile";
        public const string AccountStatus    = "v1/contractor/account-status";
        public const string GetSignature     = "v1/contractor/signature";
        public const string UpdateSignature  = "v1/contractor/update-signature";
        public const string GetTerms         = "v1/contractor/terms";
        public const string UpdateTerms      = "v1/contractor/update-terms";
        public const string CustomerDetail   = "v1/Customers/detail";
        public const string AddUpdateContract = "v1/contractor/contracts/add-update";
        public const string Contracts        = "v1/contractor/contracts";
        public const string DashboardStats   = "v1/contractor/dashboard/stats";
        public const string GovernmentId     = "v1/contractor/government-id";
        public const string Quotes           = "v1/contractor/quotes";
        public const string Invoices         = "v1/contractor/contracts/invoices";
        public const string FinancialReports = "v1/contractor/reports/financial";
        public const string Notifications    = "v1/contractor/notifications";

        /// <summary>
        /// Returns the URL used to mark a single notification as read.
        /// Server expects a POST to <c>v1/contractor/notifications/{id}/read</c>.
        /// </summary>
        public static string ReadNotification(long notificationId)
            => $"v1/contractor/notifications/{notificationId}/read";
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
