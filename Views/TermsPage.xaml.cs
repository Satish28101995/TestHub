using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class TermsPage : ContentPage
{
    private readonly TermsViewModel _vm;

    public TermsPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<TermsViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync().ConfigureAwait(true);
    }
}
