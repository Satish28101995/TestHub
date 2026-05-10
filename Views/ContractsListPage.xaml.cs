using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class ContractsListPage : ContentPage
{
    private readonly ContractsListViewModel _vm;

    public ContractsListPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<ContractsListViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync(reset: true).ConfigureAwait(true);
    }
}
