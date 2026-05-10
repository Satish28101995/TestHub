using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class NewContractPage : ContentPage
{
    private readonly NewContractViewModel _vm;

    public NewContractPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<NewContractViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync().ConfigureAwait(true);
    }

    private async void OnCustomerEmailUnfocused(object? sender, FocusEventArgs e)
    {
        // Defer slightly so the bound CustomerEmail value has been
        // committed before we hit the network.
        await _vm.LookupCustomerAsync().ConfigureAwait(true);
    }
}
