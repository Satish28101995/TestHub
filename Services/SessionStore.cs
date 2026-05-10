using System.Text.Json;
using TestHub.Models.Auth;
using TestHub.Models.Contractor;

namespace TestHub.Services;

public sealed class SessionStore : ISessionStore
{
    private const string AccessTokenKey = "th.accessToken";
    private const string AuthTokenKey   = "th.authToken";
    private const string UserKey        = "th.currentUser";

    private AccountStatusDto? _stashedAccountStatus;

    public string? AccessToken { get; private set; }
    public string? AuthorizationToken { get; private set; }
    public LoginResponse? CurrentUser { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(AccessToken) || !string.IsNullOrEmpty(AuthorizationToken);

    public async Task SaveAsync(LoginResponse user)
    {
        CurrentUser = user;
        AccessToken = user.AccessToken;
        AuthorizationToken = user.AuthorizationToken;

        try
        {
            await SecureStorage.Default.SetAsync(AccessTokenKey, user.AccessToken ?? string.Empty).ConfigureAwait(false);
            await SecureStorage.Default.SetAsync(AuthTokenKey, user.AuthorizationToken ?? string.Empty).ConfigureAwait(false);
            await SecureStorage.Default.SetAsync(UserKey, JsonSerializer.Serialize(user)).ConfigureAwait(false);
        }
        catch
        {
            // SecureStorage may be unavailable on some platforms (e.g. simulators
            // without a keychain). Tokens still live in memory for the session.
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            AccessToken        = await SecureStorage.Default.GetAsync(AccessTokenKey).ConfigureAwait(false);
            AuthorizationToken = await SecureStorage.Default.GetAsync(AuthTokenKey).ConfigureAwait(false);

            var raw = await SecureStorage.Default.GetAsync(UserKey).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(raw))
            {
                CurrentUser = JsonSerializer.Deserialize<LoginResponse>(raw);
            }
        }
        catch
        {
            // Ignore — store remains empty.
        }
    }

    public Task ClearAsync()
    {
        AccessToken = null;
        AuthorizationToken = null;
        CurrentUser = null;
        _stashedAccountStatus = null;

        try
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(AuthTokenKey);
            SecureStorage.Default.Remove(UserKey);
        }
        catch
        {
            // Ignore.
        }

        return Task.CompletedTask;
    }

    public void StashAccountStatus(AccountStatusDto? status)
        => _stashedAccountStatus = status;

    public AccountStatusDto? ConsumeAccountStatus()
    {
        var snapshot = _stashedAccountStatus;
        _stashedAccountStatus = null;
        return snapshot;
    }
}
