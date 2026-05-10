using TestHub.Models;
using TestHub.Models.Auth;

namespace TestHub.Services;

public interface IAuthService
{
    Task<ApiResult<LoginResponse>> LoginAsync(string email, string password,
        UserType userType = UserType.Contractor, CancellationToken ct = default);

    /// <summary>
    /// Submits the 4-digit OTP that was emailed to <paramref name="email"/>.
    /// On success, the auth tokens returned from the server are persisted to
    /// the local session — the same way they are after a normal login —
    /// so subsequent API calls carry the <c>AccessToken</c> +
    /// <c>Authorization: Bearer</c> headers automatically.
    /// </summary>
    Task<ApiResult<LoginResponse>> VerifyEmailOtpAsync(string email, string otp,
        UserType userType = UserType.Contractor, CancellationToken ct = default);

    /// <summary>
    /// Asks the server to send a fresh OTP to <paramref name="email"/>.
    /// </summary>
    Task<ApiResult<bool>> ResendEmailOtpAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Calls the server logout endpoint and clears the local session.
    /// The session is always cleared, even if the network call fails.
    /// </summary>
    Task<ApiResult<bool>> SignOutAsync(CancellationToken ct = default);
}
