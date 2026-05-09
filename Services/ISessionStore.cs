using TestHub.Models.Auth;

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
}
