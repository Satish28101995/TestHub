using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class SignupPage : ContentPage
{
    public SignupPage()
    {
        InitializeComponent();
        // Resolved via DI so the VM can take constructor dependencies
        // (IAuthService, etc.) — matches LoginPage / OtpVerificationPage.
        BindingContext = ServiceHelper.GetRequiredService<SignupViewModel>();
    }
}
