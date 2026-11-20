namespace ClinicBook.Domain.Common;

/// <summary>
/// Categorizes domain/application errors.
/// </summary>
public enum ErrorType
{
    /// <summary>Input or request validation failure.</summary>
    Validation = 0,

    /// <summary>Resource was not found.</summary>
    NotFound = 1,

    /// <summary>Conflict with current state (e.g. concurrency, duplicate).</summary>
    Conflict = 2,

    /// <summary>Caller is not authenticated.</summary>
    Unauthorized = 3,

    /// <summary>Caller is not permitted to perform the action.</summary>
    Forbidden = 4,

    /// <summary>Unexpected failure.</summary>
    Failure = 5
}

/// <summary>
/// Represents a typed error with a stable code and human-readable message.
/// </summary>
/// <param name="Code">Stable error code.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Type">Error category.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>No error sentinel.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Creates a validation error.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates a not-found error.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates a conflict error.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Creates an unauthorized error.</summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates a forbidden error.</summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>Creates a generic failure error.</summary>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
