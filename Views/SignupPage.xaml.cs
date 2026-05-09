using TestHub.ViewModels;

namespace TestHub.Views;

public partial class SignupPage : ContentPage
{
    public SignupPage()
    {
        InitializeComponent();
        BindingContext = new SignupViewModel();
    }
}
