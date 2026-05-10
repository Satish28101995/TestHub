using TestHub.Models;
using TestHub.Models.Auth;

namespace TestHub.Services;

public sealed class AuthService : IAuthService
{
    private readonly IApiClient _api;
    private readonly ISessionStore _session;
    private readonly IDeviceTokenProvider _deviceTokens;

    public AuthService(IApiClient api, ISessionStore session, IDeviceTokenProvider deviceTokens)
    {
        _api = api;
        _session = session;
        _deviceTokens = deviceTokens;
    }

    public async Task<ApiResult<LoginResponse>> LoginAsync(
        string email,
        string password,
        UserType userType = UserType.Contractor,
        CancellationToken ct = default)
    {
        var payload = new LoginRequest
        {
            Email = email,
            Password = password,
            DeviceToken = await _deviceTokens.GetTokenAsync(ct).ConfigureAwait(false),
            DeviceType = (int)HeaderProvider.GetDeviceType(),
            UserType = (int)userType,
        };

        var result = await _api
            .PostAsync<LoginResponse>(AppConfig.Endpoints.Login, payload, requireAuth: false, ct)
            .ConfigureAwait(false);

        // On success, persist the tokens so the next call carries
        // both the AccessToken header and Authorization: Bearer ...
        if (result.IsSuccess && result.Data is not null)
        {
            await _session.SaveAsync(result.Data).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<ApiResult<LoginResponse>> VerifyEmailOtpAsync(
        string email,
        string otp,
        UserType userType = UserType.Contractor,
        CancellationToken ct = default)
    {
        var payload = new VerifyOtpRequest
        {
            Email = email,
            Otp = otp,
            DeviceToken = await _deviceTokens.GetTokenAsync(ct).ConfigureAwait(false),
            DeviceType = (int)HeaderProvider.GetDeviceType(),
            UserType = (int)userType,
        };

        var result = await _api
            .PostAsync<LoginResponse>(AppConfig.Endpoints.VerifyOtp, payload, requireAuth: false, ct)
            .ConfigureAwait(false);

        // The verify endpoint returns the same envelope as login when
        // the user is created/activated — persist the tokens so the
        // app is fully signed in by the time we hit the dashboard.
        if (result.IsSuccess && result.Data is not null &&
            !string.IsNullOrEmpty(result.Data.AccessToken))
        {
            await _session.SaveAsync(result.Data).ConfigureAwait(false);
        }

        return result;
    }

    public Task<ApiResult<bool>> ResendEmailOtpAsync(string email, CancellationToken ct = default)
    {
        var payload = new ResendOtpRequest { Email = email };

        return _api.PostAsync<bool>(
            AppConfig.Endpoints.ResendOtp, payload, requireAuth: false, ct);
    }

    public async Task<ApiResult<bool>> SignOutAsync(CancellationToken ct = default)
    {
        ApiResult<bool> result;
        try
        {
            result = await _api
                .PostAsync<bool>(AppConfig.Endpoints.Logout, body: null, requireAuth: true, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result = ApiResult<bool>.Fail(ex.Message, 0);
        }

        // Always clear the local session so the user is signed out
        // client-side regardless of whether the server call succeeded.
        await _session.ClearAsync().ConfigureAwait(false);
        return result;
    }
}
