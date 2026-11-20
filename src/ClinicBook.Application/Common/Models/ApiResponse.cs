namespace ClinicBook.Application.Common.Models;

/// <summary>
/// Standard API response envelope for successful operations.
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>Creates a successful response.</summary>
    public ApiResponse(T? data, string? message = null)
    {
        Success = true;
        Data = data;
        Message = message;
    }

    /// <summary>Indicates success.</summary>
    public bool Success { get; init; }

    /// <summary>Response payload.</summary>
    public T? Data { get; init; }

    /// <summary>Optional informational message.</summary>
    public string? Message { get; init; }

    /// <summary>Correlation/trace identifier for diagnostics.</summary>
    public string? TraceId { get; init; }

    /// <summary>Creates a successful response.</summary>
    public static ApiResponse<T> Ok(T? data, string? message = null, string? traceId = null) =>
        new(data, message) { TraceId = traceId };
}

/// <summary>
/// Non-generic API response envelope.
/// </summary>
public sealed class ApiResponse
{
    /// <summary>Creates a response.</summary>
    public ApiResponse(bool success, string? message = null)
    {
        Success = success;
        Message = message;
    }

    /// <summary>Indicates success.</summary>
    public bool Success { get; init; }

    /// <summary>Optional informational message.</summary>
    public string? Message { get; init; }

    /// <summary>Correlation/trace identifier for diagnostics.</summary>
    public string? TraceId { get; init; }

    /// <summary>Creates a successful response.</summary>
    public static ApiResponse Ok(string? message = null, string? traceId = null) =>
        new(true, message) { TraceId = traceId };
}
