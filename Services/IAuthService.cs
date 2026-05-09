using TestHub.Models;
using TestHub.Models.Auth;

namespace TestHub.Services;

public interface IAuthService
{
    Task<ApiResult<LoginResponse>> LoginAsync(string email, string password,
        UserType userType = UserType.Contractor, CancellationToken ct = default);

    Task SignOutAsync();
}
