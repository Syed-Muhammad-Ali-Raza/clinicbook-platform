using ClinicBook.Domain.Common;

namespace ClinicBook.Domain.Entities;

/// <summary>
/// Records sensitive actions for auditing and compliance.
/// </summary>
public sealed class AuditLog : BaseEntity
{
    /// <summary>UTC time the action occurred.</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Identifier of the acting user, if known.</summary>
    public string? UserId { get; set; }

    /// <summary>Action performed (e.g. Auth.Login, Auth.RefreshTokenReuse).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Target entity type, if applicable.</summary>
    public string? EntityType { get; set; }

    /// <summary>Target entity identifier, if applicable.</summary>
    public string? EntityId { get; set; }

    /// <summary>Outcome of the action (Success/Failure/Denied).</summary>
    public string Outcome { get; set; } = "Success";

    /// <summary>Additional non-sensitive details (JSON or text).</summary>
    public string? Details { get; set; }

    /// <summary>Client IP address, if available.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Client user agent, if available.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Correlation/trace identifier.</summary>
    public string? TraceId { get; set; }
}
