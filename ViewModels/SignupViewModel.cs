using System.Text.RegularExpressions;
using System.Windows.Input;

namespace TestHub.ViewModels;

public sealed class SignupViewModel : BaseViewModel
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhonePattern = new(
        @"^[\d\s\+\-\(\)]{8,20}$",
        RegexOptions.Compiled);

    private static readonly Regex UrlPattern = new(
        @"^(https?:\/\/)?([\w\-]+\.)+[\w\-]{2,}([\/\w\-\.\?\=\&%#]*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private string _companyName = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _address = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _website = string.Empty;
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
    private string _passwordError = string.Empty;
    private string _confirmPasswordError = string.Empty;
    private string _termsError = string.Empty;

    public SignupViewModel()
    {
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
    public string PasswordError { get => _passwordError; set { if (SetProperty(ref _passwordError, value)) OnPropertyChanged(nameof(HasPasswordError)); } }
    public string ConfirmPasswordError { get => _confirmPasswordError; set { if (SetProperty(ref _confirmPasswordError, value)) OnPropertyChanged(nameof(HasConfirmPasswordError)); } }
    public string TermsError { get => _termsError; set { if (SetProperty(ref _termsError, value)) OnPropertyChanged(nameof(HasTermsError)); } }

    public bool HasCompanyNameError => !string.IsNullOrEmpty(CompanyNameError);
    public bool HasFirstNameError => !string.IsNullOrEmpty(FirstNameError);
    public bool HasLastNameError => !string.IsNullOrEmpty(LastNameError);
    public bool HasPhoneError => !string.IsNullOrEmpty(PhoneError);
    public bool HasEmailError => !string.IsNullOrEmpty(EmailError);
    public bool HasWebsiteError => !string.IsNullOrEmpty(WebsiteError);
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
            // Simulated registration / send-verification call.
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(true);

            await DisplayAlertSafeAsync(
                "Verification email sent",
                $"We have sent a verification link to {Email.Trim()}.",
                "OK").ConfigureAwait(true);

            await GoToLoginAsync().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
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

        if (!string.IsNullOrWhiteSpace(Phone) && !PhonePattern.IsMatch(Phone.Trim()))
        {
            PhoneError = "Please enter a valid phone number.";
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
