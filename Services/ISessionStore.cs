using TestHub.Models.Auth;
using TestHub.Models.Contractor;

namespace TestHub.Services;

/// <summary>
/// Persists the signed-in user's tokens and identity. Tokens are stored
/// using <see cref="SecureStorage"/> so they survive app restarts and
/// are encrypted by the platform when supported.
/// </summary>
public interface ISessionStore
{
    bool IsAuthenticated { get; }
    string? AccessToken { get; }
    string? AuthorizationToken { get; }
    LoginResponse? CurrentUser { get; }

    Task SaveAsync(LoginResponse user);
    Task LoadAsync();
    Task ClearAsync();

    /// <summary>
    /// Stash the freshly-fetched account status so the next page that needs
    /// it can read it without a duplicate network call. Lives in memory only
    /// — the value is cleared on consume and on sign-out.
    /// </summary>
    void StashAccountStatus(AccountStatusDto? status);

    /// <summary>
    /// Returns and clears the previously stashed account status. Returns
    /// <c>null</c> when nothing is pending consumption (the caller should
    /// then fetch fresh data from the API).
    /// </summary>
    AccountStatusDto? ConsumeAccountStatus();
}
