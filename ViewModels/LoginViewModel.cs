using System.Text.RegularExpressions;
using System.Windows.Input;

namespace TestHub.ViewModels;

public sealed class LoginViewModel : BaseViewModel
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;
    private bool _isPasswordHidden = true;

    public LoginViewModel()
    {
        SignInCommand = new AsyncRelayCommand(SignInAsync);
        TogglePasswordCommand = new RelayCommand(() => IsPasswordHidden = !IsPasswordHidden);
        ForgotPasswordCommand = new AsyncRelayCommand(ShowForgotPasswordAsync);
        GoogleSignInCommand = new AsyncRelayCommand(() => SocialSignInAsync("Google"));
        AppleSignInCommand = new AsyncRelayCommand(() => SocialSignInAsync("Apple"));
        SignUpCommand = new AsyncRelayCommand(ShowSignUpAsync);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public bool IsPasswordHidden
    {
        get => _isPasswordHidden;
        set => SetProperty(ref _isPasswordHidden, value);
    }

    public ICommand SignInCommand { get; }
    public ICommand TogglePasswordCommand { get; }
    public ICommand ForgotPasswordCommand { get; }
    public ICommand GoogleSignInCommand { get; }
    public ICommand AppleSignInCommand { get; }
    public ICommand SignUpCommand { get; }

    private async Task SignInAsync()
    {
        if (!ValidateInputs(out var validationError))
        {
            await DisplayAlertSafeAsync("Sign in failed", validationError, "OK").ConfigureAwait(true);
            return;
        }

        try
        {
            IsBusy = true;
            // Simulated authentication call. Replace with the real auth service.
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(true);

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//home").ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool ValidateInputs(out string error)
    {
        if (string.IsNullOrWhiteSpace(Email) || !EmailPattern.IsMatch(Email))
        {
            error = "Please enter a valid email address.";
            return false;
        }

        if (string.IsNullOrEmpty(Password))
        {
            error = "Password cannot be empty.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static Task ShowForgotPasswordAsync()
        => DisplayAlertSafeAsync(
            "Forgot Password",
            "Password reset flow is not implemented yet.",
            "OK");

    private static Task SocialSignInAsync(string provider)
        => DisplayAlertSafeAsync(
            $"{provider} Sign-In",
            $"{provider} sign-in is not implemented yet.",
            "OK");

    private static Task ShowSignUpAsync()
        => DisplayAlertSafeAsync(
            "Sign Up",
            "Sign-up flow is not implemented yet.",
            "OK");

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
