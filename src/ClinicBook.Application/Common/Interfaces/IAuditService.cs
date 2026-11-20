namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Writes audit log entries for sensitive actions.
/// </summary>
public interface IAuditService
{
    /// <summary>Records an audit event.</summary>
    Task LogAsync(
        string action,
        string outcome = "Success",
        Guid? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? traceId = null,
        CancellationToken cancellationToken = default);
}
