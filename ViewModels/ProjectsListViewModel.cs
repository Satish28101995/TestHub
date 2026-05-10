using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using TestHub.Models.Quote;
using TestHub.Services;

namespace TestHub.ViewModels;

/// <summary>
/// Backing VM for the Projects (quotes) list. Drives:
///   - paginated load against /v1/contractor/quotes (Page, PageSize, Search)
///   - infinite scroll via <see cref="LoadMoreCommand"/>
///   - search bar (re-fetches the first page when the term changes)
///   - tab/back navigation
/// </summary>
public sealed class ProjectsListViewModel : BaseViewModel
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;

    private readonly IApiClient _api;

    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;
    private int _totalCount;
    private bool _isLoadingMore;
    private string _searchTerm = string.Empty;

    public ProjectsListViewModel(IApiClient api)
    {
        _api = api;
        Items = new ObservableCollection<QuoteListItemViewModel>();
        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasMore));
        };

        BackCommand        = new AsyncRelayCommand(GoBackAsync);
        RefreshCommand     = new AsyncRelayCommand(() => LoadAsync(reset: true));
        LoadMoreCommand    = new AsyncRelayCommand(LoadMoreAsync);
        OpenProjectCommand = new AsyncRelayCommand<QuoteListItemViewModel?>(OpenProjectAsync);
        SearchCommand      = new AsyncRelayCommand(() => LoadAsync(reset: true));

        // Tab navigation
        GoHomeCommand     = new AsyncRelayCommand(() => GoToAsync("//dashboard"));
        GoInvoicesCommand = new AsyncRelayCommand(() => GoToAsync("//invoices"));
        GoReportsCommand  = new AsyncRelayCommand(() => GoToAsync("//reports"));
        GoProfileCommand  = new AsyncRelayCommand(() => GoToAsync("//profile"));
    }

    public ObservableCollection<QuoteListItemViewModel> Items { get; }

    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => !IsBusy && Items.Count == 0;

    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        private set => SetProperty(ref _isLoadingMore, value);
    }

    public bool HasMore => Items.Count < _totalCount;

    /// <summary>
    /// Two-way bound to the search bar. Setting the value does not trigger
    /// a fetch on its own; the page either commits via the
    /// <see cref="SearchCommand"/> or waits until the next pull-to-refresh.
    /// </summary>
    public string SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value ?? string.Empty);
    }

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand SearchCommand { get; }

    public ICommand GoHomeCommand { get; }
    public ICommand GoInvoicesCommand { get; }
    public ICommand GoReportsCommand { get; }
    public ICommand GoProfileCommand { get; }

    /// <summary>
    /// Fetches the first page (or refreshes the existing data when called
    /// again). Safe to call repeatedly — each call resets pagination state.
    /// </summary>
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
                .GetAsync<QuotesListResponse>(url, requireAuth: true)
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

    /// <summary>
    /// Wired to <c>RemainingItemsThresholdReachedCommand</c> on the
    /// CollectionView. Fires repeatedly as the user keeps scrolling — the
    /// guards on <see cref="IsLoadingMore"/> and <see cref="HasMore"/> make
    /// it idempotent and the page only commits on a successful response.
    /// </summary>
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
                .GetAsync<QuotesListResponse>(BuildUrl(nextPage), requireAuth: true)
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

    private void ApplyResponse(QuotesListResponse data, bool append)
    {
        if (!append)
        {
            Items.Clear();
        }

        if (data.Items is { Count: > 0 } items)
        {
            foreach (var item in items)
            {
                Items.Add(new QuoteListItemViewModel(item));
            }
        }

        _totalCount = data.TotalCount;
        _pageSize = data.PageSize > 0 ? data.PageSize : _pageSize;

        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Builds the GET URL for /v1/contractor/quotes. The API contract
    /// expects three query parameters on every call — <c>Page</c>,
    /// <c>PageSize</c>, and <c>Search</c> (empty string when no filter set).
    /// </summary>
    private string BuildUrl(int page)
    {
        var search = _searchTerm ?? string.Empty;

        var sb = new StringBuilder(AppConfig.Endpoints.Quotes);
        sb.Append("?Page=").Append(page);
        sb.Append("&PageSize=").Append(_pageSize);
        sb.Append("&Search=").Append(Uri.EscapeDataString(search));
        return sb.ToString();
    }

    private static Task GoToAsync(string route)
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync(route);

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//dashboard");

    private static Task OpenProjectAsync(QuoteListItemViewModel? item)
    {
        if (item is null)
        {
            return Task.CompletedTask;
        }

        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null
            ? Task.CompletedTask
            : page.DisplayAlertAsync(
                item.ProjectName,
                "Project details view is not implemented yet.",
                "OK");
    }

    private static Task Coming(string area)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null
            ? Task.CompletedTask
            : page.DisplayAlertAsync(area, $"{area} is not implemented yet.", "OK");
    }
}
