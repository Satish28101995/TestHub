using System.Windows.Input;
using TestHub.Models.Auth;
using TestHub.Services;

namespace TestHub.ViewModels;

/// <summary>
/// VM behind the Change Password page. Handles three password inputs
/// (current / new / confirm) with their own visibility toggles, exposes
/// the live "requirement" flags that drive the on-screen checklist, runs
/// inline validation, and posts to <c>/v1/Account/ChangePassword</c>.
/// </summary>
public sealed class ChangePasswordViewModel : BaseViewModel
{
    private const int MinPasswordLength = 8;
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;':\",.<>/?`~\\";

    private readonly IApiClient _api;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;

    private string _currentPasswordError = string.Empty;
    private string _newPasswordError = string.Empty;
    private string _confirmPasswordError = string.Empty;

    private bool _isCurrentHidden = true;
    private bool _isNewHidden = true;
    private bool _isConfirmHidden = true;

    public ChangePasswordViewModel(IApiClient api)
    {
        _api = api;

        SubmitCommand           = new AsyncRelayCommand(SubmitAsync);
        BackCommand             = new AsyncRelayCommand(GoBackAsync);
        CancelCommand           = new AsyncRelayCommand(GoBackAsync);
        ToggleCurrentCommand    = new AsyncRelayCommand(() => { IsCurrentHidden = !IsCurrentHidden; return Task.CompletedTask; });
        ToggleNewCommand        = new AsyncRelayCommand(() => { IsNewHidden     = !IsNewHidden;     return Task.CompletedTask; });
        ToggleConfirmCommand    = new AsyncRelayCommand(() => { IsConfirmHidden = !IsConfirmHidden; return Task.CompletedTask; });
    }

    // ---------- Inputs ----------

    public string CurrentPassword
    {
        get => _currentPassword;
        set
        {
            if (SetProperty(ref _currentPassword, value ?? string.Empty))
            {
                CurrentPasswordError = string.Empty;
            }
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value ?? string.Empty))
            {
                NewPasswordError = string.Empty;
                RaiseRequirementFlags();
                // Confirm validity depends on the new password too.
                if (!string.IsNullOrEmpty(_confirmPassword))
                {
                    OnPropertyChanged(nameof(PasswordsMatch));
                }
            }
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetProperty(ref _confirmPassword, value ?? string.Empty))
            {
                ConfirmPasswordError = string.Empty;
                OnPropertyChanged(nameof(PasswordsMatch));
            }
        }
    }

    // ---------- Inline error messages ----------

    public string CurrentPasswordError
    {
        get => _currentPasswordError;
        private set
        {
            if (SetProperty(ref _currentPasswordError, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasCurrentPasswordError));
            }
        }
    }

    public string NewPasswordError
    {
        get => _newPasswordError;
        private set
        {
            if (SetProperty(ref _newPasswordError, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasNewPasswordError));
            }
        }
    }

    public string ConfirmPasswordError
    {
        get => _confirmPasswordError;
        private set
        {
            if (SetProperty(ref _confirmPasswordError, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasConfirmPasswordError));
            }
        }
    }

    public bool HasCurrentPasswordError => !string.IsNullOrEmpty(CurrentPasswordError);
    public bool HasNewPasswordError     => !string.IsNullOrEmpty(NewPasswordError);
    public bool HasConfirmPasswordError => !string.IsNullOrEmpty(ConfirmPasswordError);

    // ---------- Visibility toggles ----------

    public bool IsCurrentHidden { get => _isCurrentHidden; set => SetProperty(ref _isCurrentHidden, value); }
    public bool IsNewHidden     { get => _isNewHidden;     set => SetProperty(ref _isNewHidden,     value); }
    public bool IsConfirmHidden { get => _isConfirmHidden; set => SetProperty(ref _isConfirmHidden, value); }

    // ---------- Live requirement flags (drive the checklist) ----------

    public bool HasMinLength    => _newPassword.Length >= MinPasswordLength;
    public bool HasUppercase    => _newPassword.Any(char.IsUpper);
    public bool HasLowercase    => _newPassword.Any(char.IsLower);
    public bool HasNumber       => _newPassword.Any(char.IsDigit);
    public bool HasSpecialChar  => _newPassword.Any(c => SpecialChars.Contains(c));
    public bool PasswordsMatch  =>
        !string.IsNullOrEmpty(_newPassword) &&
        string.Equals(_newPassword, _confirmPassword, StringComparison.Ordinal);

    private void RaiseRequirementFlags()
    {
        OnPropertyChanged(nameof(HasMinLength));
        OnPropertyChanged(nameof(HasUppercase));
        OnPropertyChanged(nameof(HasLowercase));
        OnPropertyChanged(nameof(HasNumber));
        OnPropertyChanged(nameof(HasSpecialChar));
    }

    // ---------- Commands ----------

    public ICommand SubmitCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ToggleCurrentCommand { get; }
    public ICommand ToggleNewCommand { get; }
    public ICommand ToggleConfirmCommand { get; }

    // ---------- Submit ----------

    private async Task SubmitAsync()
    {
        if (!Validate())
        {
            return;
        }

        try
        {
            IsBusy = true;

            var payload = new ChangePasswordRequest
            {
                OldPassword     = _currentPassword,
                Password        = _newPassword,
                ConfirmPassword = _confirmPassword,
            };

            var result = await _api.PostAsync<bool>(
                AppConfig.Endpoints.ChangePassword, payload, requireAuth: true)
                .ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                // Surface server-side validation errors against the most
                // likely field so the user can react immediately.
                var msg = string.IsNullOrWhiteSpace(result.Message)
                    ? "Could not update your password. Please try again."
                    : result.Message;

                if (msg.Contains("current", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("old", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentPasswordError = msg;
                }
                else
                {
                    await DisplayAlertSafeAsync("Could not update password", msg, "OK")
                        .ConfigureAwait(true);
                }
                return;
            }

            await DisplayAlertSafeAsync(
                "Password updated",
                "Your password has been changed successfully.",
                "OK").ConfigureAwait(true);

            ResetForm();
            await GoBackAsync().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Inline validation. Returns false on the first failure so the user's
    /// attention is drawn to one field at a time.
    /// </summary>
    private bool Validate()
    {
        var ok = true;

        if (string.IsNullOrWhiteSpace(_currentPassword))
        {
            CurrentPasswordError = "Please enter your current password.";
            ok = false;
        }

        if (string.IsNullOrWhiteSpace(_newPassword))
        {
            NewPasswordError = "Please enter a new password.";
            ok = false;
        }
        else if (!HasMinLength)
        {
            NewPasswordError = $"Use at least {MinPasswordLength} characters.";
            ok = false;
        }
        else if (!HasUppercase)
        {
            NewPasswordError = "Include at least one uppercase letter.";
            ok = false;
        }
        else if (!HasLowercase)
        {
            NewPasswordError = "Include at least one lowercase letter.";
            ok = false;
        }
        else if (!HasNumber)
        {
            NewPasswordError = "Include at least one number.";
            ok = false;
        }
        else if (!HasSpecialChar)
        {
            NewPasswordError = "Include at least one special character.";
            ok = false;
        }
        else if (string.Equals(_currentPassword, _newPassword, StringComparison.Ordinal))
        {
            NewPasswordError = "New password must be different from the current one.";
            ok = false;
        }

        if (string.IsNullOrEmpty(_confirmPassword))
        {
            ConfirmPasswordError = "Please confirm your new password.";
            ok = false;
        }
        else if (!PasswordsMatch)
        {
            ConfirmPasswordError = "Passwords do not match.";
            ok = false;
        }

        return ok;
    }

    private void ResetForm()
    {
        CurrentPassword = string.Empty;
        NewPassword     = string.Empty;
        ConfirmPassword = string.Empty;
    }

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//profile");

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
