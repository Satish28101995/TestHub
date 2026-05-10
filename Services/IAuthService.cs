using TestHub.Models;
using TestHub.Models.Auth;

namespace TestHub.Services;

public interface IAuthService
{
    Task<ApiResult<LoginResponse>> LoginAsync(string email, string password,
        UserType userType = UserType.Contractor, CancellationToken ct = default);

    /// <summary>
    /// Registers a new account against <c>POST /v1/Account/SignUp</c>.
    /// The caller is expected to populate every property on
    /// <paramref name="payload"/> that the form / device knows about;
    /// <c>userType</c> and <c>deviceType</c> are filled in automatically
    /// here so view models don't need to know about them.
    ///
    /// The server response envelope is
    /// <c>{ "data": true|false, "message": "...", "apiName": "..." }</c>
    /// — the <c>data</c> flag indicates whether the account was created
    /// and the verification email was queued. No auth tokens are issued
    /// at this stage; the user must verify the OTP first
    /// (<see cref="VerifyEmailOtpAsync"/>).
    /// </summary>
    Task<ApiResult<bool>> SignUpAsync(SignupRequest payload,
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
