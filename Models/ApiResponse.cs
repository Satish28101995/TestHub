using System.Text.Json.Serialization;

namespace TestHub.Models;

/// <summary>
/// Server envelope used by every endpoint:
/// { "data": ..., "message": "...", "apiName": "..." }
/// </summary>
public sealed class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("apiName")]
    public string? ApiName { get; set; }
}

/// <summary>
/// Result wrapper returned to view models. Carries success state, the
/// deserialized data (when present), the server message and the HTTP
/// status code so the UI can react appropriately.
/// </summary>
public sealed class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public int StatusCode { get; init; }
    public string? ApiName { get; init; }

    public static ApiResult<T> Ok(T? data, string? message, int status, string? apiName)
        => new() { IsSuccess = true, Data = data, Message = message, StatusCode = status, ApiName = apiName };

    public static ApiResult<T> Fail(string? message, int status, string? apiName = null)
        => new() { IsSuccess = false, Message = message, StatusCode = status, ApiName = apiName };
}
