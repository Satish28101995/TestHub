using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _vm;

    public ProfilePage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<ProfileViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync().ConfigureAwait(true);
    }
}
