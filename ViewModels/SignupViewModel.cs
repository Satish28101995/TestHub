using System.Text.RegularExpressions;
using System.Windows.Input;
using TestHub.Models.Auth;
using TestHub.Services;

namespace TestHub.ViewModels;

public sealed class SignupViewModel : BaseViewModel
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Phone field uses Keyboard="Numeric" + MaxLength="10" in the XAML,
    // so the input is already digits-only on every platform. The regex
    // is the server-side guarantee — exactly 10 digits, nothing else.
    private static readonly Regex PhonePattern = new(
        @"^\d{10}$",
        RegexOptions.Compiled);

    // ABN: 11 digits, no separators. Business rule from the AU ATO; the
    // checksum isn't validated client-side because the server already
    // does it on submit (avoid duplicating finicky logic in two places).
    private static readonly Regex AbnPattern = new(
        @"^\d{11}$",
        RegexOptions.Compiled);

    // Licence numbers vary by state / authority — letters, digits and a
    // few separators are common. Capped at 20 characters per the spec.
    private static readonly Regex LicensePattern = new(
        @"^[A-Za-z0-9\-\s\/]{1,20}$",
        RegexOptions.Compiled);

    private static readonly Regex UrlPattern = new(
        @"^(https?:\/\/)?([\w\-]+\.)+[\w\-]{2,}([\/\w\-\.\?\=\&%#]*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IAuthService _auth;

    private string _companyName = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _address = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _website = string.Empty;
    private string _abnNumber = string.Empty;
    private string _licenseNumber = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _logoFileName = string.Empty;
    private string _abnFileName = string.Empty;
    private bool _acceptTerms;
    private bool _isPasswordHidden = true;
    private bool _isConfirmPasswordHidden = true;

    private string _companyNameError = string.Empty;
    private string _firstNameError = string.Empty;
    private string _lastNameError = string.Empty;
    private string _phoneError = string.Empty;
    private string _emailError = string.Empty;
    private string _websiteError = string.Empty;
    private string _abnNumberError = string.Empty;
    private string _licenseNumberError = string.Empty;
    private string _passwordError = string.Empty;
    private string _confirmPasswordError = string.Empty;
    private string _termsError = string.Empty;

    public SignupViewModel(IAuthService auth)
    {
        _auth = auth;

        BackCommand = new AsyncRelayCommand(GoBackAsync);
        TogglePasswordCommand = new RelayCommand(() => IsPasswordHidden = !IsPasswordHidden);
        ToggleConfirmPasswordCommand = new RelayCommand(() => IsConfirmPasswordHidden = !IsConfirmPasswordHidden);
        UploadLogoCommand = new AsyncRelayCommand(UploadLogoAsync);
        UploadAbnCommand = new AsyncRelayCommand(UploadAbnAsync);
        VerifyEmailCommand = new AsyncRelayCommand(VerifyEmailAsync);
        SignInCommand = new AsyncRelayCommand(GoToLoginAsync);
    }

    public string CompanyName
    {
        get => _companyName;
        set { if (SetProperty(ref _companyName, value)) ClearError(nameof(CompanyNameError), v => CompanyNameError = v); }
    }

    public string FirstName
    {
        get => _firstName;
        set { if (SetProperty(ref _firstName, value)) ClearError(nameof(FirstNameError), v => FirstNameError = v); }
    }

    public string LastName
    {
        get => _lastName;
        set { if (SetProperty(ref _lastName, value)) ClearError(nameof(LastNameError), v => LastNameError = v); }
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string Phone
    {
        get => _phone;
        set { if (SetProperty(ref _phone, value)) ClearError(nameof(PhoneError), v => PhoneError = v); }
    }

    public string Email
    {
        get => _email;
        set { if (SetProperty(ref _email, value)) ClearError(nameof(EmailError), v => EmailError = v); }
    }

    public string Website
    {
        get => _website;
        set { if (SetProperty(ref _website, value)) ClearError(nameof(WebsiteError), v => WebsiteError = v); }
    }

    public string AbnNumber
    {
        get => _abnNumber;
        set { if (SetProperty(ref _abnNumber, value)) ClearError(nameof(AbnNumberError), v => AbnNumberError = v); }
    }

    public string LicenseNumber
    {
        get => _licenseNumber;
        set { if (SetProperty(ref _licenseNumber, value)) ClearError(nameof(LicenseNumberError), v => LicenseNumberError = v); }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ClearError(nameof(PasswordError), v => PasswordError = v);
                ClearError(nameof(ConfirmPasswordError), v => ConfirmPasswordError = v);
            }
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { if (SetProperty(ref _confirmPassword, value)) ClearError(nameof(ConfirmPasswordError), v => ConfirmPasswordError = v); }
    }

    public string LogoFileName
    {
        get => _logoFileName;
        set
        {
            if (SetProperty(ref _logoFileName, value))
            {
                OnPropertyChanged(nameof(HasLogo));
            }
        }
    }

    public string AbnFileName
    {
        get => _abnFileName;
        set
        {
            if (SetProperty(ref _abnFileName, value))
            {
                OnPropertyChanged(nameof(HasAbnDocument));
            }
        }
    }

    public bool AcceptTerms
    {
        get => _acceptTerms;
        set
        {
            if (SetProperty(ref _acceptTerms, value))
            {
                ClearError(nameof(TermsError), v => TermsError = v);
            }
        }
    }

    public bool IsPasswordHidden
    {
        get => _isPasswordHidden;
        set => SetProperty(ref _isPasswordHidden, value);
    }

    public bool IsConfirmPasswordHidden
    {
        get => _isConfirmPasswordHidden;
        set => SetProperty(ref _isConfirmPasswordHidden, value);
    }

    public string CompanyNameError { get => _companyNameError; set { if (SetProperty(ref _companyNameError, value)) OnPropertyChanged(nameof(HasCompanyNameError)); } }
    public string FirstNameError { get => _firstNameError; set { if (SetProperty(ref _firstNameError, value)) OnPropertyChanged(nameof(HasFirstNameError)); } }
    public string LastNameError { get => _lastNameError; set { if (SetProperty(ref _lastNameError, value)) OnPropertyChanged(nameof(HasLastNameError)); } }
    public string PhoneError { get => _phoneError; set { if (SetProperty(ref _phoneError, value)) OnPropertyChanged(nameof(HasPhoneError)); } }
    public string EmailError { get => _emailError; set { if (SetProperty(ref _emailError, value)) OnPropertyChanged(nameof(HasEmailError)); } }
    public string WebsiteError { get => _websiteError; set { if (SetProperty(ref _websiteError, value)) OnPropertyChanged(nameof(HasWebsiteError)); } }
    public string AbnNumberError { get => _abnNumberError; set { if (SetProperty(ref _abnNumberError, value)) OnPropertyChanged(nameof(HasAbnNumberError)); } }
    public string LicenseNumberError { get => _licenseNumberError; set { if (SetProperty(ref _licenseNumberError, value)) OnPropertyChanged(nameof(HasLicenseNumberError)); } }
    public string PasswordError { get => _passwordError; set { if (SetProperty(ref _passwordError, value)) OnPropertyChanged(nameof(HasPasswordError)); } }
    public string ConfirmPasswordError { get => _confirmPasswordError; set { if (SetProperty(ref _confirmPasswordError, value)) OnPropertyChanged(nameof(HasConfirmPasswordError)); } }
    public string TermsError { get => _termsError; set { if (SetProperty(ref _termsError, value)) OnPropertyChanged(nameof(HasTermsError)); } }

    public bool HasCompanyNameError => !string.IsNullOrEmpty(CompanyNameError);
    public bool HasFirstNameError => !string.IsNullOrEmpty(FirstNameError);
    public bool HasLastNameError => !string.IsNullOrEmpty(LastNameError);
    public bool HasPhoneError => !string.IsNullOrEmpty(PhoneError);
    public bool HasEmailError => !string.IsNullOrEmpty(EmailError);
    public bool HasWebsiteError => !string.IsNullOrEmpty(WebsiteError);
    public bool HasAbnNumberError => !string.IsNullOrEmpty(AbnNumberError);
    public bool HasLicenseNumberError => !string.IsNullOrEmpty(LicenseNumberError);
    public bool HasPasswordError => !string.IsNullOrEmpty(PasswordError);
    public bool HasConfirmPasswordError => !string.IsNullOrEmpty(ConfirmPasswordError);
    public bool HasTermsError => !string.IsNullOrEmpty(TermsError);

    public bool HasLogo => !string.IsNullOrEmpty(LogoFileName);
    public bool HasAbnDocument => !string.IsNullOrEmpty(AbnFileName);

    public ICommand BackCommand { get; }
    public ICommand TogglePasswordCommand { get; }
    public ICommand ToggleConfirmPasswordCommand { get; }
    public ICommand UploadLogoCommand { get; }
    public ICommand UploadAbnCommand { get; }
    public ICommand VerifyEmailCommand { get; }
    public ICommand SignInCommand { get; }

    private async Task UploadLogoAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select company logo",
                FileTypes = FilePickerFileType.Images,
            }).ConfigureAwait(true);

            if (result is null)
            {
                return;
            }

            var info = new FileInfo(result.FullPath);
            if (info.Length > 5 * 1024 * 1024)
            {
                await DisplayAlertSafeAsync("File too large", "Please pick a PNG or JPG up to 5MB.", "OK").ConfigureAwait(true);
                return;
            }

            LogoFileName = result.FileName;
        }
        catch (PermissionException)
        {
            await DisplayAlertSafeAsync("Permission needed", "Allow access to pick a company logo.", "OK").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await DisplayAlertSafeAsync("Upload failed", ex.Message, "OK").ConfigureAwait(true);
        }
    }

    private async Task UploadAbnAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Upload ABN / ACN / License document",
            }).ConfigureAwait(true);

            if (result is null)
            {
                return;
            }

            AbnFileName = result.FileName;
        }
        catch (PermissionException)
        {
            await DisplayAlertSafeAsync("Permission needed", "Allow access to pick a document.", "OK").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await DisplayAlertSafeAsync("Upload failed", ex.Message, "OK").ConfigureAwait(true);
        }
    }

    private async Task VerifyEmailAsync()
    {
        if (!ValidateAll())
        {
            return;
        }

        try
        {
            IsBusy = true;

            var payload = BuildSignupRequest();

            var result = await _auth
                .SignUpAsync(payload, UserType.Contractor)
                .ConfigureAwait(true);

            // Two failure modes to surface to the user:
            //   1. HTTP / transport-level failure  (result.IsSuccess == false)
            //   2. HTTP 200 but server returned   ("data": false)
            //      (e.g. duplicate email, invalid ABN, weak password)
            // Either way we show the server's `message` if it gave us one
            // so the user knows what to fix.
            if (!result.IsSuccess || !result.Data)
            {
                await DisplayAlertSafeAsync(
                    "Sign up failed",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "We couldn't create your account. Please try again."
                        : result.Message,
                    "OK").ConfigureAwait(true);
                return;
            }

            // data == true → account created, verification email queued.
            // Hand the user off to the OTP page. The email travels via
            // a Shell route query param so the OTP VM can call
            // /v1/Account/Email/VerifyOtp without us touching it.
            if (Shell.Current is not null)
            {
                var encoded = Uri.EscapeDataString(Email.Trim());
                await Shell.Current
                    .GoToAsync($"//otp?email={encoded}")
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertSafeAsync(
                "Sign up failed",
                ex.Message,
                "OK").ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Maps the form state onto the wire contract expected by
    /// <c>POST /v1/Account/SignUp</c>.
    ///
    /// The current signup form only captures a single free-text address
    /// and stores selected file <em>names</em> (not uploaded URLs), so
    /// the granular address fields and upload URLs are sent empty for
    /// now. When the form gains a full address picker / file-upload
    /// flow, fill these in here — no other layer needs to change.
    /// </summary>
    private SignupRequest BuildSignupRequest()
    {
        var contactName = string.Join(" ", new[] { FirstName, LastName }
            .Select(s => (s ?? string.Empty).Trim())
            .Where(s => s.Length > 0));

        return new SignupRequest
        {
            // Contact / identity
            Email = Email.Trim(),
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            CompanyName = CompanyName.Trim(),
            ContactName = contactName,
            PhoneNumber = Phone.Trim(),

            // Address — granular fields aren't captured by the form yet,
            // so we send the free-text address as `location` and leave
            // the rest empty / 0. TODO: wire to a proper address picker.
            Location = Address.Trim(),
            StreetNumber = string.Empty,
            StreetName = string.Empty,
            Suburb = string.Empty,
            Postcode = string.Empty,
            Latitude = 0,
            Longitude = 0,

            // Business identifiers — captured by the form below the
            // contact details section. The API uses the British spelling
            // "Licence" on the wire; the C# property here matches.
            AbnNumber = AbnNumber.Trim(),
            LicenceNumber = LicenseNumber.Trim(),
            Website = Website.Trim(),

            // Uploads — the form only stores file names locally; once a
            // /v1/Upload endpoint is available, push the picked files
            // there first and then assign the returned URLs here.
            LogoUrl = string.Empty,
            GovernmentIdFileUrl = string.Empty,

            // Credentials + consent
            Password = Password,
            ConfirmPassword = ConfirmPassword,
            TermsAndCondition = AcceptTerms,

            // userType + deviceType are stamped by AuthService.SignUpAsync
            // so callers can't get them wrong; the defaults here are fine.
        };
    }

    private bool ValidateAll()
    {
        var ok = true;

        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            CompanyNameError = "Company name is required.";
            ok = false;
        }

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            FirstNameError = "First name is required.";
            ok = false;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            LastNameError = "Last name is required.";
            ok = false;
        }

        if (string.IsNullOrWhiteSpace(Phone))
        {
            PhoneError = "Phone number is required.";
            ok = false;
        }
        else if (!PhonePattern.IsMatch(Phone.Trim()))
        {
            PhoneError = "Phone number must be exactly 10 digits.";
            ok = false;
        }

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

        if (!string.IsNullOrWhiteSpace(Website) && !UrlPattern.IsMatch(Website.Trim()))
        {
            WebsiteError = "Please enter a valid website URL.";
            ok = false;
        }

        if (string.IsNullOrWhiteSpace(AbnNumber))
        {
            AbnNumberError = "ABN number is required.";
            ok = false;
        }
        else if (!AbnPattern.IsMatch(AbnNumber.Trim()))
        {
            AbnNumberError = "ABN must be exactly 11 digits.";
            ok = false;
        }

        if (string.IsNullOrWhiteSpace(LicenseNumber))
        {
            LicenseNumberError = "Licence number is required.";
            ok = false;
        }
        else if (!LicensePattern.IsMatch(LicenseNumber.Trim()))
        {
            LicenseNumberError = "Licence number must be up to 20 letters or digits.";
            ok = false;
        }

        if (string.IsNullOrEmpty(Password))
        {
            PasswordError = "Password is required.";
            ok = false;
        }
        else if (Password.Length < 8)
        {
            PasswordError = "Password must be at least 8 characters.";
            ok = false;
        }

        if (string.IsNullOrEmpty(ConfirmPassword))
        {
            ConfirmPasswordError = "Please confirm your password.";
            ok = false;
        }
        else if (Password != ConfirmPassword)
        {
            ConfirmPasswordError = "Passwords do not match.";
            ok = false;
        }

        if (!AcceptTerms)
        {
            TermsError = "You must accept the terms and conditions.";
            ok = false;
        }

        return ok;
    }

    private void ClearError(string errorPropertyName, Action<string> setter)
    {
        setter(string.Empty);
        OnPropertyChanged(errorPropertyName);
    }

    private static Task GoBackAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//login");

    private static Task GoToLoginAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//login");

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
