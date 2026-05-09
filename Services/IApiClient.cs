using TestHub.Models;

namespace TestHub.Services;

/// <summary>
/// Common HTTP client used by every feature. Always returns an
/// <see cref="ApiResult{T}"/> so callers don't have to deal with raw
/// exceptions or parse JSON envelopes themselves.
/// </summary>
public interface IApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string path, bool requireAuth = true, CancellationToken ct = default);
    Task<ApiResult<T>> PostAsync<T>(string path, object? body, bool requireAuth = true, CancellationToken ct = default);
    Task<ApiResult<T>> PutAsync<T>(string path, object? body, bool requireAuth = true, CancellationToken ct = default);
    Task<ApiResult<T>> DeleteAsync<T>(string path, bool requireAuth = true, CancellationToken ct = default);
}
