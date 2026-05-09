using System.Collections.ObjectModel;
using System.Windows.Input;
using TestHub.Models.Contractor;
using TestHub.Services;

namespace TestHub.ViewModels;

public sealed class DashboardViewModel : BaseViewModel
{
    private readonly ISessionStore _session;
    private readonly IAuthService _auth;
    private readonly IApiClient _api;

    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private bool _isGovernmentIdVerified;
    private bool _isBankAccountLinked;
    private bool _isESignatureCompleted;
    private bool _isContractTermsCompleted;
    private bool _hasNotifications = true;

    // ---------- "Completed" dashboard headline stats ----------
    private string _projectsCount     = "24";
    private string _projectsTrend     = "+3 this week";
    private string _activeCount       = "8";
    private string _activeTrend       = "On track";
    private string _revenueValue      = "$68.2K";
    private string _revenueTrend      = "+24% MTD";
    private string _pendingValue      = "$12.5K";
    private string _pendingTrend      = "2 Invoices";

    public DashboardViewModel(ISessionStore session, IAuthService auth, IApiClient api)
    {
        _session = session;
        _auth = auth;
        _api = api;

        NotificationsCommand    = new AsyncRelayCommand(ShowNotificationsAsync);
        SetupBankCommand        = new AsyncRelayCommand(() => Coming("Bank Account"));
        AddESignatureCommand    = new AsyncRelayCommand(GoToSignatureAsync);
        AddContractTermsCommand = new AsyncRelayCommand(GoToTermsAsync);
        SignOutCommand          = new AsyncRelayCommand(SignOutAsync);
        RefreshCommand          = new AsyncRelayCommand(LoadAsync);

        NewQuoteCommand         = new AsyncRelayCommand(() => Coming("New Quote"));
        ProjectsCommand         = new AsyncRelayCommand(() => Coming("Projects"));
        ContractsCommand        = new AsyncRelayCommand(() => Coming("Contracts"));
        InvoicesCommand         = new AsyncRelayCommand(() => Coming("Invoices"));
        ReportsCommand          = new AsyncRelayCommand(() => Coming("Reports"));
        CompleteActionCommand   = new AsyncRelayCommand(() => Coming("Premium Verification"));
        ViewAllProjectsCommand  = new AsyncRelayCommand(() => Coming("All Projects"));
        OpenProfileCommand      = new AsyncRelayCommand(GoToProfileAsync);

        RecentProjects = new ObservableCollection<RecentProjectItem>();
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
        private set
        {
            if (SetProperty(ref _isGovernmentIdVerified, value))
            {
                OnPropertyChanged(nameof(IsOnboardingComplete));
                OnPropertyChanged(nameof(IsOnboardingPending));
            }
        }
    }

    public bool IsBankAccountLinked
    {
        get => _isBankAccountLinked;
        private set
        {
            if (SetProperty(ref _isBankAccountLinked, value))
            {
                OnPropertyChanged(nameof(IsOnboardingComplete));
                OnPropertyChanged(nameof(IsOnboardingPending));
            }
        }
    }

    public bool IsESignatureCompleted
    {
        get => _isESignatureCompleted;
        private set
        {
            if (SetProperty(ref _isESignatureCompleted, value))
            {
                OnPropertyChanged(nameof(IsOnboardingComplete));
                OnPropertyChanged(nameof(IsOnboardingPending));
            }
        }
    }

    public bool IsContractTermsCompleted
    {
        get => _isContractTermsCompleted;
        private set
        {
            if (SetProperty(ref _isContractTermsCompleted, value))
            {
                OnPropertyChanged(nameof(IsOnboardingComplete));
                OnPropertyChanged(nameof(IsOnboardingPending));
            }
        }
    }

    /// <summary>
    /// True only when ALL four onboarding flags returned by the API are
    /// true. The page swaps to the rich completed layout in that case.
    /// </summary>
    public bool IsOnboardingComplete =>
        IsGovernmentIdVerified &&
        IsBankAccountLinked &&
        IsESignatureCompleted &&
        IsContractTermsCompleted;

    public bool IsOnboardingPending => !IsOnboardingComplete;

    public bool HasNotifications
    {
        get => _hasNotifications;
        set => SetProperty(ref _hasNotifications, value);
    }

    public string ProjectsCount { get => _projectsCount; private set => SetProperty(ref _projectsCount, value); }
    public string ProjectsTrend { get => _projectsTrend; private set => SetProperty(ref _projectsTrend, value); }
    public string ActiveCount   { get => _activeCount;   private set => SetProperty(ref _activeCount,   value); }
    public string ActiveTrend   { get => _activeTrend;   private set => SetProperty(ref _activeTrend,   value); }
    public string RevenueValue  { get => _revenueValue;  private set => SetProperty(ref _revenueValue,  value); }
    public string RevenueTrend  { get => _revenueTrend;  private set => SetProperty(ref _revenueTrend,  value); }
    public string PendingValue  { get => _pendingValue;  private set => SetProperty(ref _pendingValue,  value); }
    public string PendingTrend  { get => _pendingTrend;  private set => SetProperty(ref _pendingTrend,  value); }

    public ObservableCollection<RecentProjectItem> RecentProjects { get; }

    public ICommand NotificationsCommand { get; }
    public ICommand SetupBankCommand { get; }
    public ICommand AddESignatureCommand { get; }
    public ICommand AddContractTermsCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand NewQuoteCommand { get; }
    public ICommand ProjectsCommand { get; }
    public ICommand ContractsCommand { get; }
    public ICommand InvoicesCommand { get; }
    public ICommand ReportsCommand { get; }
    public ICommand CompleteActionCommand { get; }
    public ICommand ViewAllProjectsCommand { get; }
    public ICommand OpenProfileCommand { get; }

    public async Task LoadAsync()
    {
        // Tokens may have been saved during the same app session; if not,
        // try to hydrate them from secure storage.
        if (_session.CurrentUser is null)
        {
            await _session.LoadAsync().ConfigureAwait(true);
        }

        var user = _session.CurrentUser;
        if (user is not null)
        {
            FirstName = user.FirstName ?? string.Empty;
            LastName  = user.LastName  ?? string.Empty;
        }

        await RefreshAccountStatusAsync().ConfigureAwait(true);
        EnsureSampleProjects();
    }

    private async Task RefreshAccountStatusAsync()
    {
        try
        {
            IsBusy = true;

            var result = await _api.GetAsync<AccountStatusDto>(
                AppConfig.Endpoints.AccountStatus, requireAuth: true)
                .ConfigureAwait(true);

            if (result.IsSuccess && result.Data is not null)
            {
                IsGovernmentIdVerified   = result.Data.IsGovernmentIdVerified;
                IsBankAccountLinked      = result.Data.IsBankAccountLinked;
                IsESignatureCompleted    = result.Data.IsESignatureAdded;
                IsContractTermsCompleted = result.Data.IsTermsAndConditionsAdded;
            }
            else
            {
                // If the call fails, fall back to whatever we know from the
                // login payload so the UI still shows something useful.
                var user = _session.CurrentUser;
                if (user is not null)
                {
                    IsGovernmentIdVerified = user.IsGovernmentIdVerified;
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Populates the Recent Projects list with placeholder data on the
    /// first load. Replace this with a real GET when the projects API
    /// is available.
    /// </summary>
    private void EnsureSampleProjects()
    {
        if (RecentProjects.Count > 0)
        {
            return;
        }

        RecentProjects.Add(new RecentProjectItem
        {
            Title = "Bathroom Remodel", ClientName = "Sarah Johnson",
            DateLabel = "Apr 5", Amount = "$25,000",
            DateString = "10/3/2026", ProjectType = "Random", Status = "active",
        });
        RecentProjects.Add(new RecentProjectItem
        {
            Title = "Full House Paint", ClientName = "Mike Davis",
            DateLabel = "Completed", Amount = "$25,000",
            DateString = "10/3/2026", ProjectType = "Random", Status = "completed",
        });
        RecentProjects.Add(new RecentProjectItem
        {
            Title = "Flooring Install", ClientName = "David Wilson",
            DateLabel = "Apr 10", Amount = "$25,000",
            DateString = "10/3/2026", ProjectType = "Random", Status = "pending",
        });
    }

    private static Task GoToSignatureAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//signature");

    private static Task GoToTermsAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//terms");

    private static Task GoToProfileAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//profile");

    private static Task Coming(string area) =>
        DisplayAlertSafeAsync(area, $"{area} is not implemented yet.", "OK");

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
