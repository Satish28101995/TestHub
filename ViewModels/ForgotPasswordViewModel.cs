using System.Text.RegularExpressions;
using System.Windows.Input;
using TestHub.Models.Auth;
using TestHub.Services;

namespace TestHub.ViewModels;

public sealed class ForgotPasswordViewModel : BaseViewModel
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IApiClient _api;

    private string _email = string.Empty;
    private string _emailError = string.Empty;

    public ForgotPasswordViewModel(IApiClient api)
    {
        _api = api;

        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
        BackCommand   = new AsyncRelayCommand(GoBackAsync);
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

            var payload = new ForgetPasswordRequest { Email = Email.Trim() };

            var result = await _api.PostAsync<bool>(
                AppConfig.Endpoints.ForgetPassword, payload, requireAuth: false)
                .ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                await DisplayAlertSafeAsync(
                    "Could not send reset link",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Please try again in a moment."
                        : result.Message,
                    "OK").ConfigureAwait(true);
                return;
            }

            // Server signals success via { "data": true }. Even when data
            // is false (e.g. unknown email), we deliberately show a
            // neutral message so we don't leak which addresses exist.
            var message = string.IsNullOrWhiteSpace(result.Message)
                ? $"If an account exists for {Email.Trim()}, a reset link has been sent."
                : result.Message;

            await DisplayAlertSafeAsync("Check your inbox", message, "OK").ConfigureAwait(true);
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
