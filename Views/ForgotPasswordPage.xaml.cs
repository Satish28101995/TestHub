using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.GetRequiredService<ForgotPasswordViewModel>();
    }
}
