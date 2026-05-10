using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using TestHub.Models.Contractor;
using TestHub.Models.Quote;
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
    // These are placeholders until /v1/contractor/dashboard/stats responds.
    private string _projectsCount     = "0";
    private string _projectsTrend     = "+0 this week";
    private string _contractsCount    = "0";
    private string _contractsTrend    = "On track";
    private string _revenueValue      = "$0";
    private string _revenueTrend      = "+0% MTD";
    private string _pendingValue      = "$0";
    private string _pendingTrend      = "0 invoices";

    // ---------- Action Required (government-id) banner ----------
    private bool _showActionRequired;

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
        ProjectsCommand         = new AsyncRelayCommand(GoToProjectsAsync);
        ContractsCommand        = new AsyncRelayCommand(GoToContractsAsync);
        InvoicesCommand         = new AsyncRelayCommand(GoToInvoicesAsync);
        ReportsCommand          = new AsyncRelayCommand(GoToReportsAsync);
        CompleteActionCommand   = new AsyncRelayCommand(() => Coming("Premium Verification"));
        ViewAllProjectsCommand  = new AsyncRelayCommand(GoToProjectsAsync);
        OpenProfileCommand      = new AsyncRelayCommand(GoToProfileAsync);
        CustomerLookupCommand   = new AsyncRelayCommand(() => Coming("Customer Lookup"));

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

    public string ProjectsCount  { get => _projectsCount;  private set => SetProperty(ref _projectsCount,  value); }
    public string ProjectsTrend  { get => _projectsTrend;  private set => SetProperty(ref _projectsTrend,  value); }
    public string ContractsCount { get => _contractsCount; private set => SetProperty(ref _contractsCount, value); }
    public string ContractsTrend { get => _contractsTrend; private set => SetProperty(ref _contractsTrend, value); }
    public string RevenueValue   { get => _revenueValue;   private set => SetProperty(ref _revenueValue,   value); }
    public string RevenueTrend   { get => _revenueTrend;   private set => SetProperty(ref _revenueTrend,   value); }
    public string PendingValue   { get => _pendingValue;   private set => SetProperty(ref _pendingValue,   value); }
    public string PendingTrend   { get => _pendingTrend;   private set => SetProperty(ref _pendingTrend,   value); }

    /// <summary>
    /// Drives the yellow "Action Required: Complete ID verification" card on
    /// the rich dashboard. Visible only when /v1/contractor/government-id
    /// returns a null record or a record without a fileUrl.
    /// </summary>
    public bool ShowActionRequired
    {
        get => _showActionRequired;
        private set => SetProperty(ref _showActionRequired, value);
    }

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
    public ICommand CustomerLookupCommand { get; }

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

        // Once we know onboarding is complete the rich dashboard is shown
        // and we need the headline stats, government-id banner state, plus
        // the latest 4 projects for the "Recent Projects" card. Fan all of
        // these out in parallel — none of them depends on the others.
        if (IsOnboardingComplete)
        {
            await Task.WhenAll(
                RefreshDashboardStatsAsync(),
                RefreshGovernmentIdAsync(),
                RefreshRecentProjectsAsync()
            ).ConfigureAwait(true);
        }
    }

    private async Task RefreshAccountStatusAsync()
    {
        // First try the snapshot stashed by the login flow — that way the
        // very first dashboard render after sign-in is instant and we don't
        // double-call /v1/contractor/account-status. Subsequent loads (pull
        // to refresh, returning to the dashboard) fall through to the API.
        var preloaded = _session.ConsumeAccountStatus();
        if (preloaded is not null)
        {
            ApplyAccountStatus(preloaded);
            return;
        }

        try
        {
            IsBusy = true;

            var result = await _api.GetAsync<AccountStatusDto>(
                AppConfig.Endpoints.AccountStatus, requireAuth: true)
                .ConfigureAwait(true);

            if (result.IsSuccess && result.Data is not null)
            {
                ApplyAccountStatus(result.Data);
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

    private void ApplyAccountStatus(AccountStatusDto status)
    {
        IsGovernmentIdVerified   = status.IsGovernmentIdVerified;
        IsBankAccountLinked      = status.IsBankAccountLinked;
        IsESignatureCompleted    = status.IsESignatureAdded;
        IsContractTermsCompleted = status.IsTermsAndConditionsAdded;
    }

    /// <summary>
    /// Loads <c>/v1/contractor/dashboard/stats</c> and projects the response
    /// onto the four headline tiles (Projects, Contracts, Revenue, Pending).
    /// Failures are swallowed so the page still renders with whatever stats
    /// are already on screen — typically the seeded zero values.
    /// </summary>
    private async Task RefreshDashboardStatsAsync()
    {
        try
        {
            var result = await _api.GetAsync<DashboardStatsDto>(
                AppConfig.Endpoints.DashboardStats, requireAuth: true)
                .ConfigureAwait(true);

            if (!result.IsSuccess || result.Data is null)
            {
                return;
            }

            ApplyDashboardStats(result.Data);
        }
        catch
        {
            // Network / parsing failures must not break the dashboard.
        }
    }

    private void ApplyDashboardStats(DashboardStatsDto stats)
    {
        ProjectsCount = stats.TotalProjects.ToString("N0", CultureInfo.InvariantCulture);
        ProjectsTrend = string.IsNullOrWhiteSpace(stats.ProjectsWeeklyDeltaDisplay)
            ? FormatSignedCount(stats.ProjectsWeeklyDelta, "this week")
            : stats.ProjectsWeeklyDeltaDisplay!;

        ContractsCount = stats.TotalContracts.ToString("N0", CultureInfo.InvariantCulture);
        ContractsTrend = "On track";

        RevenueValue = FormatCurrency(stats.Revenue);
        RevenueTrend = string.IsNullOrWhiteSpace(stats.RevenueMtdGrowthDisplay)
            ? FormatPercent(stats.RevenueMtdGrowthPercent, "MTD")
            : stats.RevenueMtdGrowthDisplay!;

        PendingValue = FormatCurrency(stats.PendingAmount);
        PendingTrend = stats.PendingCount == 1 ? "1 invoice" : $"{stats.PendingCount} invoices";
    }

    /// <summary>
    /// Loads <c>/v1/contractor/government-id</c>. The yellow "Action Required"
    /// card is only displayed when the API returns no record (404 / null
    /// data) or returns a record whose <c>fileUrl</c> is missing.
    /// </summary>
    private async Task RefreshGovernmentIdAsync()
    {
        try
        {
            var result = await _api.GetAsync<GovernmentIdDto>(
                AppConfig.Endpoints.GovernmentId, requireAuth: true)
                .ConfigureAwait(true);

            // No data, or data without a usable file URL => prompt upload.
            if (!result.IsSuccess || result.Data is null)
            {
                ShowActionRequired = true;
                return;
            }

            ShowActionRequired = !result.Data.HasUploadedId;
        }
        catch
        {
            // Surface the banner on transient failures too — better to nudge
            // the user once more than to silently hide the call to action.
            ShowActionRequired = true;
        }
    }

    private static string FormatCurrency(decimal value)
    {
        // Match the design which shows whole-dollar amounts with a single
        // decimal place (e.g. "$672.0", "$250.0"). For very large values we
        // collapse to "$1.2K" / "$3.4M" to keep the tile readable.
        var abs = Math.Abs(value);
        if (abs >= 1_000_000m)
        {
            return string.Format(CultureInfo.InvariantCulture, "${0:0.0}M", value / 1_000_000m);
        }
        if (abs >= 10_000m)
        {
            return string.Format(CultureInfo.InvariantCulture, "${0:0.0}K", value / 1_000m);
        }
        return string.Format(CultureInfo.InvariantCulture, "${0:0.0}", value);
    }

    private static string FormatSignedCount(int delta, string suffix)
    {
        var sign = delta > 0 ? "+" : string.Empty;
        return string.Format(CultureInfo.InvariantCulture, "{0}{1} {2}", sign, delta, suffix);
    }

    private static string FormatPercent(decimal value, string suffix)
    {
        var sign = value > 0 ? "+" : string.Empty;
        return string.Format(CultureInfo.InvariantCulture, "{0}{1:0}% {2}", sign, value, suffix);
    }

    /// <summary>
    /// Populates the Recent Projects list with placeholder data on the
    /// first load. Replace this with a real GET when the projects API
    /// is available.
    /// </summary>
    /// <summary>
    /// Loads the latest 4 quotes for the "Recent Projects" card on the
    /// rich dashboard. We always fetch <c>Page=1, PageSize=4</c> so the
    /// list mirrors the Projects screen but only shows the four newest
    /// rows. Failures are swallowed — if the API call fails we leave the
    /// previously cached list (or empty list) in place rather than
    /// surfacing an error in the middle of the dashboard.
    /// </summary>
    private async Task RefreshRecentProjectsAsync()
    {
        try
        {
            var url = $"{AppConfig.Endpoints.Quotes}?Page=1&PageSize=4&Search=";

            var result = await _api.GetAsync<QuotesListResponse>(url, requireAuth: true)
                .ConfigureAwait(true);

            if (!result.IsSuccess || result.Data?.Items is null)
            {
                return;
            }

            RecentProjects.Clear();
            foreach (var item in result.Data.Items.Take(4))
            {
                RecentProjects.Add(MapToRecentProject(item));
            }
        }
        catch
        {
            // Network/parse failures must not break the dashboard.
        }
    }

    /// <summary>
    /// Maps a <see cref="QuoteListItem"/> from the quotes endpoint into the
    /// presentation-ready <see cref="RecentProjectItem"/> used by the
    /// dashboard list. Status is normalised to one of "active" |
    /// "completed" | "pending" so the badge colours light up correctly.
    /// </summary>
    private static RecentProjectItem MapToRecentProject(QuoteListItem item)
    {
        var amount = item.TotalAmount ?? item.Amount ?? 0m;
        var rawDate = item.StartDate ?? item.CreatedDate ?? item.CreatedAtUtc ?? item.UpdatedAtUtc;
        var date = rawDate?.ToLocalTime();

        var status = NormaliseStatus(item.Status);
        var dateLabel = status == "completed"
            ? "Completed"
            : (date is null ? "—" : date.Value.ToString("MMM d", CultureInfo.InvariantCulture));

        return new RecentProjectItem
        {
            Title       = string.IsNullOrWhiteSpace(item.ProjectName) ? "Untitled Project" : item.ProjectName!,
            ClientName  = string.IsNullOrWhiteSpace(item.CustomerName) ? "—" : item.CustomerName!,
            DateLabel   = dateLabel,
            Amount      = string.Format(CultureInfo.InvariantCulture, "${0:N0}", amount),
            DateString  = date is null ? "—" : date.Value.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
            ProjectType = string.IsNullOrWhiteSpace(item.ProjectType) ? "—" : item.ProjectType!,
            Status      = status,
        };
    }

    private static string NormaliseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "active";
        }

        var normalised = status.Trim().ToLowerInvariant();
        return normalised switch
        {
            "complete" or "completed" or "done"          => "completed",
            "pending"  or "draft"     or "outstanding"   => "pending",
            _                                             => "active",
        };
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

    private static Task GoToContractsAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//contracts");

    private static Task GoToProjectsAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//projects");

    private static Task GoToInvoicesAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//invoices");

    private static Task GoToReportsAsync()
        => Shell.Current is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync("//reports");

    private static Task Coming(string area) =>
        DisplayAlertSafeAsync(area, $"{area} is not implemented yet.", "OK");

    private static Task ShowNotificationsAsync() =>
        Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//notifications");

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
