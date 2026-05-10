using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestHub.Services;
using TestHub.ViewModels;
using TestHub.Views;

namespace TestHub
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // ---------- HTTP + services ----------
            builder.Services.AddSingleton<HttpClient>(_ => new HttpClient
            {
                BaseAddress = new Uri(AppConfig.BaseUrl),
                Timeout = TimeSpan.FromSeconds(30),
            });

            builder.Services.AddSingleton<ISessionStore, SessionStore>();
            builder.Services.AddSingleton<IHeaderProvider, HeaderProvider>();
            builder.Services.AddSingleton<IApiClient, ApiClient>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IRememberedAccountStore, RememberedAccountStore>();

            // ---------- View models ----------
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<ForgotPasswordViewModel>();
            builder.Services.AddTransient<SignupViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<SignatureViewModel>();
            builder.Services.AddTransient<TermsViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<NewContractViewModel>();
            builder.Services.AddTransient<ContractsListViewModel>();
            builder.Services.AddTransient<ProjectsListViewModel>();
            builder.Services.AddTransient<InvoicesListViewModel>();
            builder.Services.AddTransient<ReportsViewModel>();
            builder.Services.AddTransient<ChangePasswordViewModel>();
            builder.Services.AddTransient<NotificationsViewModel>();

            // ---------- Pages ----------
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<ForgotPasswordPage>();
            builder.Services.AddTransient<SignupPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<SignaturePage>();
            builder.Services.AddTransient<TermsPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<NewContractPage>();
            builder.Services.AddTransient<ContractsListPage>();
            builder.Services.AddTransient<ProjectsListPage>();
            builder.Services.AddTransient<InvoicesListPage>();
            builder.Services.AddTransient<ReportsPage>();
            builder.Services.AddTransient<ChangePasswordPage>();
            builder.Services.AddTransient<NotificationsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
