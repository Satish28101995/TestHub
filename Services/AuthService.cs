using TestHub.Models;
using TestHub.Models.Auth;

namespace TestHub.Services;

public sealed class AuthService : IAuthService
{
    private readonly IApiClient _api;
    private readonly ISessionStore _session;

    public AuthService(IApiClient api, ISessionStore session)
    {
        _api = api;
        _session = session;
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
            DeviceToken = await GetDeviceTokenAsync().ConfigureAwait(false),
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

    /// <summary>
    /// Returns the platform's push notification token if one is available.
    /// Wire your FCM/APNS handler to provide this; for now we fall back to
    /// the stable per-install identifier so the server can still associate
    /// the request with a device.
    /// </summary>
    private static Task<string> GetDeviceTokenAsync()
    {
        try
        {
            // Replace this with your real push token retrieval.
            return Task.FromResult(string.Empty);
        }
        catch
        {
            return Task.FromResult(string.Empty);
        }
    }
}
