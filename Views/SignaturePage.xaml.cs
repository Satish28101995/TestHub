using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class SignaturePage : ContentPage
{
    private readonly SignatureViewModel _vm;

    public SignaturePage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<SignatureViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync().ConfigureAwait(true);
    }

    private void OnClearTapped(object? sender, TappedEventArgs e)
    {
        SignaturePad.Clear();
        _vm.IsDirty = false;
    }

    private void OnRotateTapped(object? sender, TappedEventArgs e)
    {
        SignaturePad.RotateBy(90f);
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        if (SignaturePad.IsEmpty && !_vm.HasExistingSignature)
        {
            await DisplayAlertAsync("Signature required",
                "Please sign in the box before submitting.", "OK").ConfigureAwait(true);
            return;
        }

        var fileName = _vm.BuildSignatureFileName();
        await _vm.SubmitAsync(fileName).ConfigureAwait(true);
    }
}
