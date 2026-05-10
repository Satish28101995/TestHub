using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _vm;

    public ReportsPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<ReportsViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync().ConfigureAwait(true);
    }
}
