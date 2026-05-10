using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using TestHub.Models.Contract;
using TestHub.Services;

namespace TestHub.ViewModels;

/// <summary>
/// Backing VM for the Contracts list. Drives:
///   - paginated load against /v1/contractor/contracts (Page, PageSize, Status, Search)
///   - summary tiles (active / pending / completed)
///   - infinite scroll (RemainingItemsThresholdReached fires LoadMoreCommand)
///   - Add Contract → New Contract page
/// </summary>
public sealed class ContractsListViewModel : BaseViewModel
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const string DefaultStatus = "All";

    private readonly IApiClient _api;

    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;
    private int _totalCount;
    private bool _isLoadingMore;
    private string _searchTerm = string.Empty;
    private string _statusFilter = DefaultStatus;

    private int _activeCount;
    private int _pendingCount;
    private int _completedCount;

    public ContractsListViewModel(IApiClient api)
    {
        _api = api;
        Items = new ObservableCollection<ContractListItemViewModel>();
        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasMore));
        };

        BackCommand          = new AsyncRelayCommand(GoBackAsync);
        AddContractCommand   = new AsyncRelayCommand(GoToNewContractAsync);
        RefreshCommand       = new AsyncRelayCommand(() => LoadAsync(reset: true));
        LoadMoreCommand      = new AsyncRelayCommand(LoadMoreAsync);
        OpenContractCommand  = new AsyncRelayCommand<ContractListItemViewModel?>(OpenContractAsync);
    }

    public ObservableCollection<ContractListItemViewModel> Items { get; }

    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => !IsBusy && Items.Count == 0;

    public int ActiveCount
    {
        get => _activeCount;
        private set => SetProperty(ref _activeCount, value);
    }

    public int PendingCount
    {
        get => _pendingCount;
        private set => SetProperty(ref _pendingCount, value);
    }

    public int CompletedCount
    {
        get => _completedCount;
        private set => SetProperty(ref _completedCount, value);
    }

    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        private set => SetProperty(ref _isLoadingMore, value);
    }

    public bool HasMore => Items.Count < _totalCount;

    public string SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value ?? string.Empty);
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set => SetProperty(ref _statusFilter,
            string.IsNullOrWhiteSpace(value) ? DefaultStatus : value);
    }

    public ICommand BackCommand { get; }
    public ICommand AddContractCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand OpenContractCommand { get; }

    /// <summary>
    /// Fetches the first page (or refreshes the existing data when called again).
    /// Safe to call repeatedly — each call resets pagination state.
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
                .GetAsync<ContractsListResponse>(url, requireAuth: true)
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
                .GetAsync<ContractsListResponse>(BuildUrl(nextPage), requireAuth: true)
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

    private void ApplyResponse(ContractsListResponse data, bool append)
    {
        if (!append)
        {
            Items.Clear();
        }

        if (data.Items is { Count: > 0 } items)
        {
            foreach (var item in items)
            {
                Items.Add(new ContractListItemViewModel(item));
            }
        }

        _totalCount = data.TotalCount;
        _pageSize = data.PageSize > 0 ? data.PageSize : _pageSize;

        if (data.Summary is not null)
        {
            ActiveCount    = data.Summary.Active;
            PendingCount   = data.Summary.Pending;
            CompletedCount = data.Summary.Completed;
        }

        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Builds the GET URL for /v1/contractor/contracts. The API contract
    /// expects all four query parameters on every call — <c>Page</c>,
    /// <c>PageSize</c>, <c>Status</c> (defaults to "All"), and <c>Search</c>
    /// (empty string when no filter is set).
    /// </summary>
    private string BuildUrl(int page)
    {
        var status = string.IsNullOrWhiteSpace(_statusFilter) ? DefaultStatus : _statusFilter;
        var search = _searchTerm ?? string.Empty;

        var sb = new StringBuilder(AppConfig.Endpoints.Contracts);
        sb.Append("?Page=").Append(page);
        sb.Append("&PageSize=").Append(_pageSize);
        //sb.Append("&Status=").Append(Uri.EscapeDataString(status));
        //sb.Append("&Search=").Append(Uri.EscapeDataString(search));
        return sb.ToString();
    }

    private static Task GoToNewContractAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//newcontract");

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//dashboard");

    private static Task OpenContractAsync(ContractListItemViewModel? item)
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
                "Contract details view is not implemented yet.",
                "OK");
    }
}
