using System.Text.RegularExpressions;
using System.Windows.Input;
using TestHub.Models.Auth;
using TestHub.Services;

namespace TestHub.ViewModels;

public sealed class LoginViewModel : BaseViewModel
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IAuthService _auth;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;
    private bool _isPasswordHidden = true;
    private string _emailError = string.Empty;
    private string _passwordError = string.Empty;

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;

        SignInCommand = new AsyncRelayCommand(SignInAsync);
        TogglePasswordCommand = new RelayCommand(() => IsPasswordHidden = !IsPasswordHidden);
        ForgotPasswordCommand = new AsyncRelayCommand(GoToForgotPasswordAsync);
        GoogleSignInCommand = new AsyncRelayCommand(() => SocialSignInAsync("Google"));
        AppleSignInCommand = new AsyncRelayCommand(() => SocialSignInAsync("Apple"));
        SignUpCommand = new AsyncRelayCommand(GoToSignUpAsync);
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

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                PasswordError = string.Empty;
                OnPropertyChanged(nameof(HasPasswordError));
            }
        }
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

    public string PasswordError
    {
        get => _passwordError;
        set
        {
            if (SetProperty(ref _passwordError, value))
            {
                OnPropertyChanged(nameof(HasPasswordError));
            }
        }
    }

    public bool HasEmailError => !string.IsNullOrEmpty(EmailError);
    public bool HasPasswordError => !string.IsNullOrEmpty(PasswordError);

    public ICommand SignInCommand { get; }
    public ICommand TogglePasswordCommand { get; }
    public ICommand ForgotPasswordCommand { get; }
    public ICommand GoogleSignInCommand { get; }
    public ICommand AppleSignInCommand { get; }
    public ICommand SignUpCommand { get; }

    private async Task SignInAsync()
    {
        if (!ValidateInputs())
        {
            return;
        }

        try
        {
            IsBusy = true;

            var result = await _auth
                .LoginAsync(Email.Trim(), Password, UserType.Contractor)
                .ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                await DisplayAlertSafeAsync("Sign in failed",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Could not sign you in. Please try again."
                        : result.Message,
                    "OK").ConfigureAwait(true);
                return;
            }

            // Tokens are already persisted by AuthService. Reset the
            // password field before leaving the page.
            Password = string.Empty;

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//dashboard").ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool ValidateInputs()
    {
        var ok = true;

        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailError = "Email is required.";
            ok = false;
        }
        else if (!EmailPattern.IsMatch(Email.Trim()))
        {
            EmailError = "Please enter a valid email address.";
            ok = false;
        }
        else
        {
            EmailError = string.Empty;
        }

        if (string.IsNullOrEmpty(Password))
        {
            PasswordError = "Password is required.";
            ok = false;
        }
        else if (Password.Length < 6)
        {
            PasswordError = "Password must be at least 6 characters.";
            ok = false;
        }
        else
        {
            PasswordError = string.Empty;
        }

        return ok;
    }

    private static Task GoToForgotPasswordAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//forgotpassword");

    private static Task GoToSignUpAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//signup");

    private static Task SocialSignInAsync(string provider)
        => DisplayAlertSafeAsync(
            $"{provider} Sign-In",
            $"{provider} sign-in is not implemented yet.",
            "OK");

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
