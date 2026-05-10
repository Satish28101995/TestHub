using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class InvoicesListPage : ContentPage
{
    private readonly InvoicesListViewModel _vm;

    public InvoicesListPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<InvoicesListViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync(reset: true).ConfigureAwait(true);
    }
}
