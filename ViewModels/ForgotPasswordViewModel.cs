using System.Text.RegularExpressions;
using System.Windows.Input;

namespace TestHub.ViewModels;

public sealed class ForgotPasswordViewModel : BaseViewModel
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private string _email = string.Empty;
    private string _emailError = string.Empty;

    public ForgotPasswordViewModel()
    {
        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
        BackCommand = new AsyncRelayCommand(GoBackAsync);
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                EmailError = string.Empty;
                OnPropertyChanged(nameof(HasEmailError));
            }
        }
    }

    public string EmailError
    {
        get => _emailError;
        set
        {
            if (SetProperty(ref _emailError, value))
            {
                OnPropertyChanged(nameof(HasEmailError));
            }
        }
    }

    public bool HasEmailError => !string.IsNullOrEmpty(EmailError);

    public ICommand SubmitCommand { get; }
    public ICommand BackCommand { get; }

    private async Task SubmitAsync()
    {
        if (!ValidateEmail())
        {
            return;
        }

        try
        {
            IsBusy = true;
            // Simulated reset link request. Wire to your auth backend.
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(true);

            await DisplayAlertSafeAsync(
                "Check your inbox",
                $"If an account exists for {Email.Trim()}, a reset link has been sent.",
                "OK").ConfigureAwait(true);

            await GoBackAsync().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailError = "Email is required.";
            return false;
        }

        if (!EmailPattern.IsMatch(Email.Trim()))
        {
            EmailError = "Please enter a valid email address.";
            return false;
        }

        EmailError = string.Empty;
        return true;
    }

    private static Task GoBackAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//login");

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
