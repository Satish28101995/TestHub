using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using TestHub.Models.Invoice;
using TestHub.Services;

namespace TestHub.ViewModels;

/// <summary>
/// Backing VM for the Invoices list. Drives:
///   - paginated load against /v1/contractor/contracts/invoices
///     (Page, PageSize, Status, Search)
///   - infinite scroll via <see cref="LoadMoreCommand"/>
///   - status chip filter (All / Paid / Sent) — re-fetches on chip tap
///   - search input — re-fetches when user has typed ≥ 3 chars (or
///     cleared the box back to empty), debounced ~350 ms
///   - header summary tiles + "Outstanding Invoices" banner
/// </summary>
public sealed class InvoicesListViewModel : BaseViewModel
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const string StatusAll = "All";
    private const string StatusPaid = "Paid";
    private const string StatusSent = "Sent";
    private const int SearchMinLength = 3;
    private const int SearchDebounceMs = 350;

    private readonly IApiClient _api;

    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;
    private int _totalCount;
    private bool _isLoadingMore;
    private string _searchTerm = string.Empty;
    private string _statusFilter = StatusAll;

    private InvoicesSummary _summary = new();
    private CancellationTokenSource? _searchCts;

    public InvoicesListViewModel(IApiClient api)
    {
        _api = api;

        Items = new ObservableCollection<InvoiceListItemViewModel>();
        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasMore));
        };

        BackCommand        = new AsyncRelayCommand(GoBackAsync);
        CreateCommand      = new AsyncRelayCommand(() => Coming("Create Invoice"));
        RefreshCommand     = new AsyncRelayCommand(() => LoadAsync(reset: true));
        LoadMoreCommand    = new AsyncRelayCommand(LoadMoreAsync);
        OpenInvoiceCommand = new AsyncRelayCommand<InvoiceListItemViewModel?>(OpenInvoiceAsync);
        DownloadPdfCommand = new AsyncRelayCommand<InvoiceListItemViewModel?>(DownloadPdfAsync);
        ResendCommand      = new AsyncRelayCommand<InvoiceListItemViewModel?>(ResendAsync);
        FilterCommand      = new AsyncRelayCommand<string?>(ApplyFilterAsync);

        // Tab navigation
        GoHomeCommand     = new AsyncRelayCommand(() => GoToAsync("//dashboard"));
        GoProjectsCommand = new AsyncRelayCommand(() => GoToAsync("//projects"));
        GoReportsCommand  = new AsyncRelayCommand(() => GoToAsync("//reports"));
        GoProfileCommand  = new AsyncRelayCommand(() => GoToAsync("//profile"));
    }

    public ObservableCollection<InvoiceListItemViewModel> Items { get; }

    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => !IsBusy && Items.Count == 0;

    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        private set => SetProperty(ref _isLoadingMore, value);
    }

    public bool HasMore => Items.Count < _totalCount;

    /// <summary>
    /// Two-way bound to the search Entry. Triggers a reload only when
    /// the term has at least <see cref="SearchMinLength"/> characters,
    /// or when the user clears the field. The fetch is debounced so we
    /// don't fire one request per keystroke.
    /// </summary>
    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            var next = value ?? string.Empty;
            if (SetProperty(ref _searchTerm, next))
            {
                _ = ScheduleSearchAsync(next);
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        private set
        {
            if (SetProperty(ref _statusFilter, string.IsNullOrWhiteSpace(value) ? StatusAll : value))
            {
                OnPropertyChanged(nameof(IsAllSelected));
                OnPropertyChanged(nameof(IsPaidSelected));
                OnPropertyChanged(nameof(IsSentSelected));
            }
        }
    }

    public bool IsAllSelected  => string.Equals(_statusFilter, StatusAll,  StringComparison.OrdinalIgnoreCase);
    public bool IsPaidSelected => string.Equals(_statusFilter, StatusPaid, StringComparison.OrdinalIgnoreCase);
    public bool IsSentSelected => string.Equals(_statusFilter, StatusSent, StringComparison.OrdinalIgnoreCase);

    public int TotalInvoices => _summary.TotalCount;
    public string TotalPaidDisplay => FormatCompactCurrency(_summary.PaidAmount);
    public bool ShowOutstandingBanner => _summary.OutstandingCount > 0 || _summary.OutstandingAmount > 0m;
    public string OutstandingAmountDisplay =>
        string.Format(CultureInfo.InvariantCulture, "${0:N0} pending payment", _summary.OutstandingAmount);

    public ICommand BackCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand OpenInvoiceCommand { get; }
    public ICommand DownloadPdfCommand { get; }
    public ICommand ResendCommand { get; }
    public ICommand FilterCommand { get; }

    public ICommand GoHomeCommand { get; }
    public ICommand GoProjectsCommand { get; }
    public ICommand GoReportsCommand { get; }
    public ICommand GoProfileCommand { get; }

    public async Task LoadAsync(bool reset = true)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;

            if (reset)
            {
                _page = DefaultPage;
                Items.Clear();
                _totalCount = 0;
                OnPropertyChanged(nameof(HasMore));
            }

            var url = BuildUrl(_page);
            var result = await _api
                .GetAsync<InvoicesListResponse>(url, requireAuth: true)
                .ConfigureAwait(true);

            if (!result.IsSuccess || result.Data is null)
            {
                return;
            }

            ApplyResponse(result.Data, append: !reset);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private async Task LoadMoreAsync()
    {
        if (IsLoadingMore || IsBusy || !HasMore)
        {
            return;
        }

        try
        {
            IsLoadingMore = true;

            var nextPage = _page + 1;
            var result = await _api
                .GetAsync<InvoicesListResponse>(BuildUrl(nextPage), requireAuth: true)
                .ConfigureAwait(true);

            if (!result.IsSuccess || result.Data is null)
            {
                return;
            }

            ApplyResponse(result.Data, append: true);
            _page = nextPage;
        }
        finally
        {
            IsLoadingMore = false;
            OnPropertyChanged(nameof(HasMore));
        }
    }

    private async Task ApplyFilterAsync(string? status)
    {
        var next = string.IsNullOrWhiteSpace(status) ? StatusAll : status;
        if (string.Equals(next, _statusFilter, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        StatusFilter = next;
        await LoadAsync(reset: true).ConfigureAwait(true);
    }

    /// <summary>
    /// Debounces search input. Fires a reload only when:
    ///   - the trimmed term is empty (user cleared the box), OR
    ///   - the trimmed term has at least <see cref="SearchMinLength"/> chars.
    /// Anything in between (1–2 chars) is intentionally ignored to stop
    /// us spamming the server.
    /// </summary>
    private async Task ScheduleSearchAsync(string term)
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;

        try
        {
            await Task.Delay(SearchDebounceMs, cts.Token).ConfigureAwait(true);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested)
        {
            return;
        }

        var trimmed = (term ?? string.Empty).Trim();
        if (trimmed.Length != 0 && trimmed.Length < SearchMinLength)
        {
            return;
        }

        await LoadAsync(reset: true).ConfigureAwait(true);
    }

    private void ApplyResponse(InvoicesListResponse data, bool append)
    {
        if (!append)
        {
            Items.Clear();
        }

        if (data.Items is { Count: > 0 } items)
        {
            foreach (var item in items)
            {
                Items.Add(new InvoiceListItemViewModel(item));
            }
        }

        _totalCount = data.TotalCount;
        _pageSize = data.PageSize > 0 ? data.PageSize : _pageSize;

        if (data.Summary is not null)
        {
            _summary = data.Summary;
            OnPropertyChanged(nameof(TotalInvoices));
            OnPropertyChanged(nameof(TotalPaidDisplay));
            OnPropertyChanged(nameof(ShowOutstandingBanner));
            OnPropertyChanged(nameof(OutstandingAmountDisplay));
        }

        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Builds the GET URL with all four expected query parameters.
    /// <c>Search</c> is only included once it satisfies the minimum length
    /// rule (or has been cleared to empty), keeping us aligned with the
    /// trigger logic in <see cref="ScheduleSearchAsync"/>.
    /// </summary>
    private string BuildUrl(int page)
    {
        var status = string.IsNullOrWhiteSpace(_statusFilter) ? StatusAll : _statusFilter;
        var search = (_searchTerm ?? string.Empty).Trim();
        if (search.Length > 0 && search.Length < SearchMinLength)
        {
            search = string.Empty;
        }

        var sb = new StringBuilder(AppConfig.Endpoints.Invoices);
        sb.Append("?Page=").Append(page);
        sb.Append("&PageSize=").Append(_pageSize);
        sb.Append("&Status=").Append(Uri.EscapeDataString(status));
        sb.Append("&Search=").Append(Uri.EscapeDataString(search));
        return sb.ToString();
    }

    private static string FormatCompactCurrency(decimal value)
    {
        var abs = Math.Abs(value);
        if (abs >= 1_000_000m)
        {
            return string.Format(CultureInfo.InvariantCulture, "${0:0.0}M", value / 1_000_000m);
        }
        if (abs >= 1_000m)
        {
            return string.Format(CultureInfo.InvariantCulture, "${0:0.0}K", value / 1_000m);
        }
        return string.Format(CultureInfo.InvariantCulture, "${0:0.##}", value);
    }

    private static Task GoToAsync(string route)
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync(route);

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//dashboard");

    private static Task OpenInvoiceAsync(InvoiceListItemViewModel? item)
        => DisplayAlertSafeAsync(
            item?.InvoiceNumber ?? "Invoice",
            "Invoice details view is not implemented yet.",
            "OK");

    private static Task DownloadPdfAsync(InvoiceListItemViewModel? item)
        => DisplayAlertSafeAsync(
            item?.InvoiceNumber ?? "Invoice",
            "PDF download is not implemented yet.",
            "OK");

    private static Task ResendAsync(InvoiceListItemViewModel? item)
        => DisplayAlertSafeAsync(
            item?.InvoiceNumber ?? "Invoice",
            "Resend is not implemented yet.",
            "OK");

    private static Task Coming(string area)
        => DisplayAlertSafeAsync(area, $"{area} is not implemented yet.", "OK");

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
