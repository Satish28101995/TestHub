using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.GetRequiredService<LoginViewModel>();
    }
}
