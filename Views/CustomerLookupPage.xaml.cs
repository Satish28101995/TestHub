using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class CustomerLookupPage : ContentPage
{
    private readonly CustomerLookupViewModel _vm;

    public CustomerLookupPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<CustomerLookupViewModel>();
        BindingContext = _vm;
    }
}
