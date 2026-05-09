using System.Windows.Input;
using TestHub.Services;

namespace TestHub.ViewModels;

public sealed class DashboardViewModel : BaseViewModel
{
    private readonly ISessionStore _session;
    private readonly IAuthService _auth;

    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private bool _isGovernmentIdVerified;
    private bool _isBankAccountLinked;
    private bool _isESignatureCompleted;
    private bool _isContractTermsCompleted;
    private bool _hasNotifications = true;

    public DashboardViewModel(ISessionStore session, IAuthService auth)
    {
        _session = session;
        _auth = auth;

        NotificationsCommand   = new AsyncRelayCommand(ShowNotificationsAsync);
        SetupBankCommand       = new AsyncRelayCommand(() => Coming("Bank Account"));
        AddESignatureCommand   = new AsyncRelayCommand(GoToSignatureAsync);
        AddContractTermsCommand = new AsyncRelayCommand(GoToTermsAsync);
        SignOutCommand         = new AsyncRelayCommand(SignOutAsync);
        RefreshCommand         = new AsyncRelayCommand(LoadAsync);
    }

    public string FirstName
    {
        get => _firstName;
        private set
        {
            if (SetProperty(ref _firstName, value))
            {
                OnPropertyChanged(nameof(FullName));
            }
        }
    }

    public string LastName
    {
        get => _lastName;
        private set
        {
            if (SetProperty(ref _lastName, value))
            {
                OnPropertyChanged(nameof(FullName));
            }
        }
    }

    public string FullName => string.Join(" ",
        new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

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

    public bool HasNotifications
    {
        get => _hasNotifications;
        set => SetProperty(ref _hasNotifications, value);
    }

    public ICommand NotificationsCommand { get; }
    public ICommand SetupBankCommand { get; }
    public ICommand AddESignatureCommand { get; }
    public ICommand AddContractTermsCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand RefreshCommand { get; }

    public async Task LoadAsync()
    {
        // Tokens may have been saved during the same app session; if not,
        // try to hydrate them from secure storage.
        if (_session.CurrentUser is null)
        {
            await _session.LoadAsync().ConfigureAwait(true);
        }

        var user = _session.CurrentUser;
        if (user is null)
        {
            return;
        }

        FirstName = user.FirstName ?? string.Empty;
        LastName  = user.LastName  ?? string.Empty;
        IsGovernmentIdVerified = user.IsGovernmentIdVerified;

        // The login payload only confirms identity verification. The other
        // statuses come from a richer profile call you can wire later.
        // For now they default to "pending" so the UI reflects an
        // onboarding-in-progress state.
    }

    private static Task GoToSignatureAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//signature");

    private static Task GoToTermsAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//terms");

    private static Task Coming(string area) =>
        DisplayAlertSafeAsync(area, $"{area} setup is not implemented yet.", "OK");

    private static Task ShowNotificationsAsync() =>
        DisplayAlertSafeAsync("Notifications", "You have no new notifications.", "OK");

    private async Task SignOutAsync()
    {
        await _auth.SignOutAsync().ConfigureAwait(true);
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("//login").ConfigureAwait(true);
        }
    }

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
