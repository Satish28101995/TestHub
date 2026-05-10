using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class NotificationsPage : ContentPage
{
    private readonly NotificationsViewModel _vm;

    public NotificationsPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<NotificationsViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync(reset: true).ConfigureAwait(true);
    }
}
