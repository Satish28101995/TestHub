using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using TestHub.Models.Notification;
using TestHub.Services;

namespace TestHub.ViewModels;

/// <summary>
/// Backing VM for the Notifications page. Drives:
///   - paginated load against /v1/contractor/notifications
///     (pageNumber, pageSize)
///   - infinite scroll via <see cref="LoadMoreCommand"/>
///   - row tap ⇒ POST /v1/contractor/notifications/{id}/read
///     (only for rows the user has not already opened)
/// </summary>
public sealed class NotificationsViewModel : BaseViewModel
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;

    private readonly IApiClient _api;

    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;
    private int _totalCount;
    private int _totalPages;
    private bool _isLoadingMore;

    public NotificationsViewModel(IApiClient api)
    {
        _api = api;
        Items = new ObservableCollection<NotificationItemViewModel>();
        Items.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(HasMore));
        };

        BackCommand     = new AsyncRelayCommand(GoBackAsync);
        RefreshCommand  = new AsyncRelayCommand(() => LoadAsync(reset: true));
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync);
        OpenCommand     = new AsyncRelayCommand<NotificationItemViewModel?>(OpenAsync);
    }

    public ObservableCollection<NotificationItemViewModel> Items { get; }

    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => !IsBusy && Items.Count == 0;

    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        private set => SetProperty(ref _isLoadingMore, value);
    }

    /// <summary>
    /// True while there are still pages left on the server. The list
    /// uses both <c>totalCount</c> and <c>totalPages</c> as a signal —
    /// totalPages is authoritative when present, totalCount is the
    /// fallback.
    /// </summary>
    public bool HasMore
    {
        get
        {
            if (_totalPages > 0)
            {
                return _page < _totalPages;
            }

            return Items.Count < _totalCount;
        }
    }

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand OpenCommand { get; }

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
                _totalPages = 0;
                OnPropertyChanged(nameof(HasMore));
            }

            var url = BuildUrl(_page);
            var result = await _api
                .GetAsync<NotificationsListResponse>(url, requireAuth: true)
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
    /// CollectionView. Guards on <see cref="IsLoadingMore"/> and
    /// <see cref="HasMore"/> make it idempotent. Page numbers are only
    /// committed after a successful response so failures don't stall
    /// future fetches.
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
                .GetAsync<NotificationsListResponse>(BuildUrl(nextPage), requireAuth: true)
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

    private void ApplyResponse(NotificationsListResponse data, bool append)
    {
        if (!append)
        {
            Items.Clear();
        }

        if (data.Notifications is { Count: > 0 } items)
        {
            foreach (var item in items)
            {
                Items.Add(new NotificationItemViewModel(item));
            }
        }

        _totalCount = data.TotalCount;
        _totalPages = data.TotalPages;
        if (data.PageSize > 0)
        {
            _pageSize = data.PageSize;
        }

        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Builds the GET URL for /v1/contractor/notifications.
    /// The endpoint expects <c>pageNumber</c> + <c>pageSize</c> on
    /// every call.
    /// </summary>
    private string BuildUrl(int pageNumber)
    {
        var sb = new StringBuilder(AppConfig.Endpoints.Notifications);
        sb.Append("?pageNumber=").Append(pageNumber);
        sb.Append("&pageSize=").Append(_pageSize);
        return sb.ToString();
    }

    /// <summary>
    /// Row tap handler. Marks the notification as read on the server
    /// (only when the row is currently unread — already-read taps are a
    /// no-op so we never double-call the endpoint). Once the call
    /// succeeds the row's local state is updated so the gold accent and
    /// title weight update without a full refresh.
    /// </summary>
    private async Task OpenAsync(NotificationItemViewModel? item)
    {
        if (item is null || !item.IsUnread)
        {
            return;
        }

        try
        {
            var url = AppConfig.Endpoints.ReadNotification(item.NotificationId);
            var result = await _api
                .PostAsync<object>(url, body: null, requireAuth: true)
                .ConfigureAwait(true);

            // We only flip the local flag once the server has acknowledged
            // the read — that way a flaky network leaves the row in a
            // recoverable state.
            if (result.IsSuccess)
            {
                item.MarkAsRead();
            }
        }
        catch
        {
            // Tap should never throw — silent failure keeps the UI
            // responsive and the row still tappable on the next try.
        }
    }

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//dashboard");
}
