using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;

    public DashboardPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<DashboardViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync().ConfigureAwait(true);
    }
}
