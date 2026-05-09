using System.Windows.Input;
using TestHub.Models.Contractor;
using TestHub.Services;

namespace TestHub.ViewModels;

public sealed class ProfileViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private readonly IAuthService _auth;
    private readonly ISessionStore _session;

    private string _fullName = string.Empty;
    private string _email = string.Empty;
    private string? _profileImage;
    private bool _isVerified;

    private string _phoneNumber = string.Empty;
    private string _bankAccountMasked = "•••• •••• •••• ----";
    private bool _hasBankAccount;

    private bool _isGovernmentIdVerified;
    private bool _isBankAccountLinked;
    private bool _isESignatureCompleted;
    private bool _isContractTermsCompleted;

    private string _premiumStatus = "Active · Renews Apr 17, 2026";
    private int _disputeTicketCount = 2;

    public ProfileViewModel(IApiClient api, IAuthService auth, ISessionStore session)
    {
        _api = api;
        _auth = auth;
        _session = session;

        EditProfileCommand    = new AsyncRelayCommand(() => Coming("Edit Profile"));
        ChangePasswordCommand = new AsyncRelayCommand(() => Coming("Change Password"));
        ManageBankCommand     = new AsyncRelayCommand(() => Coming("Bank Account"));
        OpenPremiumCommand    = new AsyncRelayCommand(() => Coming("Premium Plan"));
        OpenDisputesCommand   = new AsyncRelayCommand(() => Coming("Dispute Tickets"));
        OpenFaqCommand        = new AsyncRelayCommand(() => Coming("FAQ & Support"));
        AddESignatureCommand  = new AsyncRelayCommand(GoToSignatureAsync);
        AddTermsCommand       = new AsyncRelayCommand(GoToTermsAsync);
        SignOutCommand        = new AsyncRelayCommand(SignOutAsync);
        DeleteAccountCommand  = new AsyncRelayCommand(DeleteAccountAsync);
        BackCommand           = new AsyncRelayCommand(GoBackAsync);
    }

    public string FullName
    {
        get => _fullName;
        private set
        {
            if (SetProperty(ref _fullName, value))
            {
                OnPropertyChanged(nameof(HasName));
            }
        }
    }

    public bool HasName => !string.IsNullOrWhiteSpace(FullName);

    public string Email
    {
        get => _email;
        private set => SetProperty(ref _email, value);
    }

    public string? ProfileImage
    {
        get => _profileImage;
        private set
        {
            if (SetProperty(ref _profileImage, value))
            {
                OnPropertyChanged(nameof(HasProfileImage));
            }
        }
    }

    public bool HasProfileImage => !string.IsNullOrEmpty(ProfileImage);

    public bool IsVerified
    {
        get => _isVerified;
        private set => SetProperty(ref _isVerified, value);
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        private set
        {
            if (SetProperty(ref _phoneNumber, value))
            {
                OnPropertyChanged(nameof(HasPhoneNumber));
            }
        }
    }

    public bool HasPhoneNumber => !string.IsNullOrWhiteSpace(PhoneNumber);

    public string BankAccountMasked
    {
        get => _bankAccountMasked;
        private set => SetProperty(ref _bankAccountMasked, value);
    }

    public bool HasBankAccount
    {
        get => _hasBankAccount;
        private set => SetProperty(ref _hasBankAccount, value);
    }

    public bool IsGovernmentIdVerified
    {
        get => _isGovernmentIdVerified;
        private set => SetProperty(ref _isGovernmentIdVerified, value);
    }

    public bool IsBankAccountLinked
    {
        get => _isBankAccountLinked;
        private set => SetProperty(ref _isBankAccountLinked, value);
    }

    public bool IsESignatureCompleted
    {
        get => _isESignatureCompleted;
        private set => SetProperty(ref _isESignatureCompleted, value);
    }

    public bool IsContractTermsCompleted
    {
        get => _isContractTermsCompleted;
        private set => SetProperty(ref _isContractTermsCompleted, value);
    }

    public string PremiumStatus
    {
        get => _premiumStatus;
        private set => SetProperty(ref _premiumStatus, value);
    }

    public int DisputeTicketCount
    {
        get => _disputeTicketCount;
        private set
        {
            if (SetProperty(ref _disputeTicketCount, value))
            {
                OnPropertyChanged(nameof(HasDisputeTickets));
                OnPropertyChanged(nameof(DisputeTicketLabel));
            }
        }
    }

    public bool HasDisputeTickets => DisputeTicketCount > 0;
    public string DisputeTicketLabel => DisputeTicketCount.ToString();

    public ICommand EditProfileCommand { get; }
    public ICommand ChangePasswordCommand { get; }
    public ICommand ManageBankCommand { get; }
    public ICommand OpenPremiumCommand { get; }
    public ICommand OpenDisputesCommand { get; }
    public ICommand OpenFaqCommand { get; }
    public ICommand AddESignatureCommand { get; }
    public ICommand AddTermsCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand DeleteAccountCommand { get; }
    public ICommand BackCommand { get; }

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;

            // Hydrate from local session first so the page never feels empty
            // while the network request is in flight.
            HydrateFromSession();

            // Fire both reads concurrently — they're independent.
            var profileTask = _api.GetAsync<ProfileDto>(
                AppConfig.Endpoints.Profile, requireAuth: true);
            var statusTask  = _api.GetAsync<AccountStatusDto>(
                AppConfig.Endpoints.AccountStatus, requireAuth: true);

            await Task.WhenAll(profileTask, statusTask).ConfigureAwait(true);

            var profile = profileTask.Result;
            if (profile.IsSuccess && profile.Data is not null)
            {
                ApplyProfile(profile.Data);
            }

            var status = statusTask.Result;
            if (status.IsSuccess && status.Data is not null)
            {
                IsGovernmentIdVerified   = status.Data.IsGovernmentIdVerified;
                IsBankAccountLinked      = status.Data.IsBankAccountLinked;
                IsESignatureCompleted    = status.Data.IsESignatureAdded;
                IsContractTermsCompleted = status.Data.IsTermsAndConditionsAdded;
            }

            // Verified pill on the header reflects ID verification.
            IsVerified = IsGovernmentIdVerified;
            HasBankAccount = IsBankAccountLinked;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void HydrateFromSession()
    {
        var user = _session.CurrentUser;
        if (user is null)
        {
            return;
        }

        FullName = string.Join(" ",
            new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        Email = user.Email ?? string.Empty;
        ProfileImage = user.ProfileImage;
        IsVerified = user.IsGovernmentIdVerified;
        IsGovernmentIdVerified = user.IsGovernmentIdVerified;
    }

    private void ApplyProfile(ProfileDto p)
    {
        FullName = string.Join(" ",
            new[] { p.FirstName, p.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(p.ContactName))
        {
            FullName = p.ContactName!;
        }

        if (!string.IsNullOrWhiteSpace(p.Email))      Email = p.Email!;
        if (!string.IsNullOrWhiteSpace(p.ProfileImage)) ProfileImage = p.ProfileImage;
        if (!string.IsNullOrWhiteSpace(p.PhoneNumber)) PhoneNumber = p.PhoneNumber!;
    }

    private async Task SignOutAsync()
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        if (page is not null)
        {
            var confirm = await page.DisplayAlertAsync(
                "Sign out",
                "Are you sure you want to sign out?",
                "Sign Out",
                "Cancel").ConfigureAwait(true);

            if (!confirm)
            {
                return;
            }
        }

        try
        {
            IsBusy = true;
            await _auth.SignOutAsync().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("//login").ConfigureAwait(true);
        }
    }

    private async Task DeleteAccountAsync()
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        if (page is null)
        {
            return;
        }

        var confirm = await page.DisplayAlertAsync(
            "Delete account",
            "This will permanently delete your account. This action cannot be undone.",
            "Delete",
            "Cancel").ConfigureAwait(true);

        if (!confirm)
        {
            return;
        }

        await page.DisplayAlertAsync("Coming soon",
            "Account deletion is not implemented yet.", "OK").ConfigureAwait(true);
    }

    private static Task GoToSignatureAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//signature");

    private static Task GoToTermsAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//terms");

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//dashboard");

    private static Task Coming(string area) =>
        DisplayAlertSafeAsync(area, $"{area} is not implemented yet.", "OK");

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
