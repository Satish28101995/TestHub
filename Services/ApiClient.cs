using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TestHub.Models;

namespace TestHub.Services;

public sealed class ApiClient : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly IHeaderProvider _headers;

    public ApiClient(HttpClient http, IHeaderProvider headers)
    {
        _http = http;
        _headers = headers;
    }

    public Task<ApiResult<T>> GetAsync<T>(string path, bool requireAuth = true, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Get, path, body: null, requireAuth, ct);

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body, bool requireAuth = true, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Post, path, body, requireAuth, ct);

    public Task<ApiResult<T>> PutAsync<T>(string path, object? body, bool requireAuth = true, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Put, path, body, requireAuth, ct);

    public Task<ApiResult<T>> DeleteAsync<T>(string path, bool requireAuth = true, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Delete, path, body: null, requireAuth, ct);

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        bool requireAuth,
        CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, NormalizePath(path));

            if (body is not null)
            {
                var json = JsonSerializer.Serialize(body, JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            _headers.Attach(request, requireAuth);

            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            var raw = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(raw))
            {
                return response.IsSuccessStatusCode
                    ? ApiResult<T>.Ok(default, message: null, status: (int)response.StatusCode, apiName: null)
                    : ApiResult<T>.Fail(BuildHttpError(response), (int)response.StatusCode);
            }

            ApiResponse<T>? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<ApiResponse<T>>(raw, JsonOptions);
            }
            catch (JsonException ex)
            {
                return ApiResult<T>.Fail(
                    $"Could not parse server response: {ex.Message}",
                    (int)response.StatusCode);
            }

            var apiName = envelope?.ApiName;
            var message = envelope?.Message;

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.Fail(
                    string.IsNullOrWhiteSpace(message) ? BuildHttpError(response) : message,
                    (int)response.StatusCode,
                    apiName);
            }

            return ApiResult<T>.Ok(envelope is not null ? envelope.Data : default,
                message, (int)response.StatusCode, apiName);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiResult<T>.Fail("The request timed out. Please try again.", 408);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Fail($"Network error: {ex.Message}", 0);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Fail($"Unexpected error: {ex.Message}", 0);
        }
    }

    private static string NormalizePath(string path)
        => string.IsNullOrEmpty(path) ? string.Empty : path.TrimStart('/');

    private static string BuildHttpError(HttpResponseMessage response)
        => $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
}
