using ClinicBook.Domain.Common;

namespace ClinicBook.Domain.Entities;

/// <summary>
/// Represents an opaque refresh token issued to a user session.
/// Only the hash is persisted; the raw token is returned to the client once.
/// </summary>
public sealed class RefreshToken : AuditableEntity
{
    /// <summary>User identifier that owns this token.</summary>
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the refresh token value.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Token family identifier used for reuse detection/rotation chains.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>UTC time when the token was issued.</summary>
    public DateTime IssuedAtUtc { get; set; }

    /// <summary>UTC time when the token expires.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>UTC time when the token was revoked, if any.</summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Reason for revocation, if any.</summary>
    public string? RevokedReason { get; set; }

    /// <summary>Hash of the replacement token, if rotated.</summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>Client device information.</summary>
    public string? DeviceInfo { get; set; }

    /// <summary>IP address associated with issuance/use.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Indicates whether the token is active at the specified UTC time.</summary>
    /// <param name="utcNow">Current UTC time.</param>
    /// <returns><c>true</c> if not revoked and not expired; otherwise <c>false</c>.</returns>
    public bool IsActiveAt(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;
}
