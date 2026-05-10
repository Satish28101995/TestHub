using System.Web;
using System.Windows.Input;
using TestHub.Models.Auth;
using TestHub.Services;

namespace TestHub.ViewModels;

/// <summary>
/// Backing VM for the post-signup OTP verification page. Drives the
/// 4-digit code entry, hits <c>/v1/Account/Email/VerifyOtp</c> on
/// submit, and routes the user to the dashboard on success.
/// </summary>
public sealed class OtpVerificationViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IAuthService _auth;

    private string _email = string.Empty;
    private string _digit1 = string.Empty;
    private string _digit2 = string.Empty;
    private string _digit3 = string.Empty;
    private string _digit4 = string.Empty;
    private string _digit5 = string.Empty;
    private string _digit6 = string.Empty;
    private string _errorMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isResending;

    public OtpVerificationViewModel(IAuthService auth)
    {
        _auth = auth;

        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
        ResendCommand = new AsyncRelayCommand(ResendAsync);
        BackCommand   = new AsyncRelayCommand(GoBackAsync);
    }

    /// <summary>
    /// The email the OTP was emailed to. Populated by Shell when the
    /// page is navigated to with <c>?email=…</c> in the route, and shown
    /// (masked) under the "OTP is sent to your email id" subtitle.
    /// </summary>
    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(MaskedEmail));
                OnPropertyChanged(nameof(SubtitleText));
            }
        }
    }

    /// <summary>
    /// Subtitle shown beneath the page title. Renders as
    /// "OTP is sent to your email id" by default, and includes the
    /// masked email if one is available so the user can confirm
    /// where to look.
    /// </summary>
    public string SubtitleText =>
        string.IsNullOrWhiteSpace(_email)
            ? "OTP is sent to your email id"
            : $"OTP is sent to {MaskedEmail}";

    public string MaskedEmail
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_email))
            {
                return string.Empty;
            }

            var at = _email.IndexOf('@');
            if (at <= 1)
            {
                return _email;
            }

            // Show the first character + the rest of the email, masking
            // the middle of the local part. e.g. "j****@gmail.com".
            var local = _email[..at];
            var domain = _email[at..];
            return local.Length <= 2
                ? local + domain
                : local[0] + new string('*', Math.Max(1, local.Length - 1)) + domain;
        }
    }

    // ------------------------------------------------------------------
    // Per-digit setters. Each setter clamps to the last digit typed so
    // copy-paste of the full code distributes correctly: when the user
    // pastes "123456" into box 1 the property only keeps "1", and the
    // other boxes pick up their own characters via the page's paste
    // handler. The clamp also stops accidental multi-character input
    // from breaking the layout.
    // ------------------------------------------------------------------
    public string Digit1 { get => _digit1; set => SetDigit(ref _digit1, value, nameof(Digit1)); }
    public string Digit2 { get => _digit2; set => SetDigit(ref _digit2, value, nameof(Digit2)); }
    public string Digit3 { get => _digit3; set => SetDigit(ref _digit3, value, nameof(Digit3)); }
    public string Digit4 { get => _digit4; set => SetDigit(ref _digit4, value, nameof(Digit4)); }
    public string Digit5 { get => _digit5; set => SetDigit(ref _digit5, value, nameof(Digit5)); }
    public string Digit6 { get => _digit6; set => SetDigit(ref _digit6, value, nameof(Digit6)); }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrWhiteSpace(_statusMessage);

    public bool IsResending
    {
        get => _isResending;
        private set => SetProperty(ref _isResending, value);
    }

    public ICommand SubmitCommand { get; }
    public ICommand ResendCommand { get; }
    public ICommand BackCommand { get; }

    /// <summary>
    /// Resets all 6 digit boxes — used after a failed verification or
    /// after a resend so the user starts with empty inputs.
    /// </summary>
    public void ClearDigits()
    {
        Digit1 = string.Empty;
        Digit2 = string.Empty;
        Digit3 = string.Empty;
        Digit4 = string.Empty;
        Digit5 = string.Empty;
        Digit6 = string.Empty;
    }

    /// <summary>
    /// Concatenated 6-digit OTP. Returns an empty string if any of the
    /// boxes are blank so the validation can short-circuit.
    /// </summary>
    public string Otp =>
        string.IsNullOrEmpty(_digit1) || string.IsNullOrEmpty(_digit2) ||
        string.IsNullOrEmpty(_digit3) || string.IsNullOrEmpty(_digit4) ||
        string.IsNullOrEmpty(_digit5) || string.IsNullOrEmpty(_digit6)
            ? string.Empty
            : string.Concat(_digit1, _digit2, _digit3, _digit4, _digit5, _digit6);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("email", out var raw) && raw is string email)
        {
            // Shell url-decodes once, but some platforms double-encode the
            // '@' as %2540 — handle both gracefully so the email survives.
            try { Email = HttpUtility.UrlDecode(email); }
            catch { Email = email; }
        }
    }

    private async Task SubmitAsync()
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        var code = Otp;
        if (code.Length != 6)
        {
            ErrorMessage = "Please enter the 6-digit code from your email.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_email))
        {
            ErrorMessage = "Email is missing — please go back and try again.";
            return;
        }

        try
        {
            IsBusy = true;

            var result = await _auth
                .VerifyEmailOtpAsync(_email, code)
                .ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "We couldn't verify that code. Please try again."
                    : result.Message!;
                ClearDigits();
                return;
            }

            // The verify endpoint returns a login envelope that AuthService
            // already persisted. Hop to the dashboard — the OnAppearing
            // logic will pick the right onboarding / completed layout.
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//dashboard").ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResendAsync()
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(_email))
        {
            ErrorMessage = "Email is missing — please go back and try again.";
            return;
        }

        try
        {
            IsResending = true;

            var result = await _auth.ResendEmailOtpAsync(_email).ConfigureAwait(true);

            if (result.IsSuccess)
            {
                StatusMessage = $"A new code has been sent to {MaskedEmail}.";
                ClearDigits();
            }
            else
            {
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "We couldn't resend the code. Please try again."
                    : result.Message!;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsResending = false;
        }
    }

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//signup");

    /// <summary>
    /// Strips everything but the last numeric character of the input
    /// so each box always holds a single digit. This is the heart of
    /// the auto-advance behaviour driven by the page code-behind.
    /// </summary>
    private void SetDigit(ref string field, string? incoming, string propertyName)
    {
        var sanitized = string.IsNullOrEmpty(incoming) ? string.Empty : OnlyLastDigit(incoming);
        if (field == sanitized)
        {
            return;
        }

        field = sanitized;
        OnPropertyChanged(propertyName);

        // Clear any previous error state once the user starts typing
        // again — keeps the red text from sticking after a retry.
        if (HasError && !string.IsNullOrEmpty(sanitized))
        {
            ErrorMessage = string.Empty;
        }
    }

    private static string OnlyLastDigit(string value)
    {
        for (var i = value.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(value[i]))
            {
                return value[i].ToString();
            }
        }

        return string.Empty;
    }
}
