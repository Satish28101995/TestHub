using TestHub.Models;
using TestHub.Models.Auth;

namespace TestHub.Services;

public interface IAuthService
{
    Task<ApiResult<LoginResponse>> LoginAsync(string email, string password,
        UserType userType = UserType.Contractor, CancellationToken ct = default);

    /// <summary>
    /// Calls the server logout endpoint and clears the local session.
    /// The session is always cleared, even if the network call fails.
    /// </summary>
    Task<ApiResult<bool>> SignOutAsync(CancellationToken ct = default);
}
