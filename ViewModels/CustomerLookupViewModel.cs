using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using TestHub.Models.Customer;
using TestHub.Services;

namespace TestHub.ViewModels;

/// <summary>
/// Backing VM for the Customer Lookup page. The page starts in an
/// "idle" state showing only the search card; the API is only called
/// once the user types something into the search box and taps the
/// Search button. Subsequent searches replace the result list.
/// </summary>
public sealed class CustomerLookupViewModel : BaseViewModel
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;

    private readonly IApiClient _api;

    private string _searchTerm = string.Empty;
    private string _committedSearch = string.Empty;
    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;
    private int _totalCount;
    private bool _hasSearched;
    private bool _isLoadingMore;

    private int _summaryTotalCount;
    private decimal _summaryAverage;

    public CustomerLookupViewModel(IApiClient api)
    {
        _api = api;
        Items = new ObservableCollection<CustomerReviewItemViewModel>();
        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasMore));
            OnPropertyChanged(nameof(ShowResults));
            OnPropertyChanged(nameof(ShowEmpty));
        };

        BackCommand     = new AsyncRelayCommand(GoBackAsync);
        SearchCommand   = new AsyncRelayCommand(RunSearchAsync);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync);
        ClearCommand    = new AsyncRelayCommand(ClearAsync);
    }

    public ObservableCollection<CustomerReviewItemViewModel> Items { get; }

    /// <summary>
    /// Two-way bound to the search box. Setting the value alone does
    /// not trigger an API call — the page either commits via the
    /// Search button or via Return-to-search on the entry.
    /// </summary>
    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (SetProperty(ref _searchTerm, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(CanSearch));
            }
        }
    }

    /// <summary>
    /// True only when there is at least one non-whitespace character
    /// in the search box. The Search button binds its <c>IsEnabled</c>
    /// here so a tap with an empty box is impossible.
    /// </summary>
    public bool CanSearch => !string.IsNullOrWhiteSpace(_searchTerm);

    /// <summary>
    /// Flag flipped to true the first time the user runs a search.
    /// Drives the visibility of the results / empty state, so the
    /// page stays clean when the screen first loads.
    /// </summary>
    public bool HasSearched
    {
        get => _hasSearched;
        private set
        {
            if (SetProperty(ref _hasSearched, value))
            {
                OnPropertyChanged(nameof(ShowResults));
                OnPropertyChanged(nameof(ShowEmpty));
            }
        }
    }

    public bool HasItems => Items.Count > 0;

    /// <summary>True when the user has searched and we got rows.</summary>
    public bool ShowResults => HasSearched && HasItems;

    /// <summary>True when the user has searched but no rows came back.</summary>
    public bool ShowEmpty => HasSearched && !HasItems && !IsBusy;

    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        private set => SetProperty(ref _isLoadingMore, value);
    }

    public bool HasMore => Items.Count < _totalCount;

    // ------------------------------------------------------------------
    // Summary derived from the response. The page shows these in the
    // "Reviews & Ratings" results header — only meaningful after the
    // first successful search.
    // ------------------------------------------------------------------
    public int SummaryTotalCount
    {
        get => _summaryTotalCount;
        private set
        {
            if (SetProperty(ref _summaryTotalCount, value))
            {
                OnPropertyChanged(nameof(SummaryReviewsLabel));
            }
        }
    }

    public decimal SummaryAverage
    {
        get => _summaryAverage;
        private set
        {
            if (SetProperty(ref _summaryAverage, value))
            {
                OnPropertyChanged(nameof(SummaryAverageDisplay));
                OnPropertyChanged(nameof(SummaryStar1Filled));
                OnPropertyChanged(nameof(SummaryStar2Filled));
                OnPropertyChanged(nameof(SummaryStar3Filled));
                OnPropertyChanged(nameof(SummaryStar4Filled));
                OnPropertyChanged(nameof(SummaryStar5Filled));
            }
        }
    }

    public string SummaryAverageDisplay =>
        _summaryAverage.ToString("0.0", CultureInfo.InvariantCulture);

    public string SummaryReviewsLabel =>
        _summaryTotalCount == 1 ? "(1 Review)" : $"({_summaryTotalCount} Reviews)";

    public bool SummaryStar1Filled => _summaryAverage >= 0.5m;
    public bool SummaryStar2Filled => _summaryAverage >= 1.5m;
    public bool SummaryStar3Filled => _summaryAverage >= 2.5m;
    public bool SummaryStar4Filled => _summaryAverage >= 3.5m;
    public bool SummaryStar5Filled => _summaryAverage >= 4.5m;

    public ICommand BackCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand ClearCommand { get; }

    /// <summary>
    /// Runs a fresh search using the current <see cref="SearchTerm"/>.
    /// Clears any previous results and resets pagination state.
    /// </summary>
    private async Task RunSearchAsync()
    {
        if (IsBusy || !CanSearch)
        {
            return;
        }

        try
        {
            IsBusy = true;
            HasSearched = true;
            _committedSearch = _searchTerm.Trim();
            _page = DefaultPage;
            Items.Clear();
            _totalCount = 0;
            OnPropertyChanged(nameof(HasMore));

            var url = BuildUrl(_page);
            var result = await _api
                .GetAsync<CustomerReviewsResponse>(url, requireAuth: true)
                .ConfigureAwait(true);

            if (!result.IsSuccess || result.Data is null)
            {
                ApplySummary(null);
                OnPropertyChanged(nameof(ShowEmpty));
                return;
            }

            ApplyResponse(result.Data, append: false);
            ApplySummary(result.Data.Summary);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(ShowResults));
            OnPropertyChanged(nameof(ShowEmpty));
        }
    }

    /// <summary>
    /// Wired to <c>RemainingItemsThresholdReachedCommand</c> on the
    /// CollectionView. Guards on <see cref="IsLoadingMore"/> and
    /// <see cref="HasMore"/> make it idempotent. Page numbers are only
    /// committed on a successful response.
    /// </summary>
    private async Task LoadMoreAsync()
    {
        if (!HasSearched || IsLoadingMore || IsBusy || !HasMore)
        {
            return;
        }

        try
        {
            IsLoadingMore = true;

            var nextPage = _page + 1;
            var result = await _api
                .GetAsync<CustomerReviewsResponse>(BuildUrl(nextPage), requireAuth: true)
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

    private Task ClearAsync()
    {
        SearchTerm = string.Empty;
        Items.Clear();
        _totalCount = 0;
        _page = DefaultPage;
        ApplySummary(null);
        HasSearched = false;
        return Task.CompletedTask;
    }

    private void ApplyResponse(CustomerReviewsResponse data, bool append)
    {
        if (!append)
        {
            Items.Clear();
        }

        if (data.Items is { Count: > 0 } items)
        {
            foreach (var item in items)
            {
                Items.Add(new CustomerReviewItemViewModel(item));
            }
        }

        _totalCount = data.TotalCount;
        if (data.PageSize > 0)
        {
            _pageSize = data.PageSize;
        }

        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(HasItems));
    }

    private void ApplySummary(CustomerReviewsSummary? summary)
    {
        SummaryTotalCount = summary?.TotalCount ?? 0;
        SummaryAverage    = summary?.TotalAverage ?? 0m;
    }

    /// <summary>
    /// Builds the GET URL with <c>Page</c>, <c>PageSize</c>, and
    /// <c>Search</c> query params (the API expects all three on every
    /// call).
    /// </summary>
    private string BuildUrl(int page)
    {
        var sb = new StringBuilder(AppConfig.Endpoints.CustomerReviews);
        sb.Append("?Page=").Append(page);
        sb.Append("&PageSize=").Append(_pageSize);
        sb.Append("&Search=").Append(Uri.EscapeDataString(_committedSearch));
        return sb.ToString();
    }

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//dashboard");
}
