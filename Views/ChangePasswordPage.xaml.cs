using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class ChangePasswordPage : ContentPage
{
    public ChangePasswordPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.GetRequiredService<ChangePasswordViewModel>();
    }
}
