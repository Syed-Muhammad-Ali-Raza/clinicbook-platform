using ClinicBook.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// Application user for ASP.NET Core Identity.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>, IAuditableEntityMarker
{
    /// <summary>User's first name.</summary>
    public string? FirstName { get; set; }

    /// <summary>User's last name.</summary>
    public string? LastName { get; set; }

    /// <summary>Optional clinic scope for clinic-scoped users.</summary>
    public Guid? ClinicId { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Creator identifier.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>UTC update timestamp.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Last updater identifier.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Soft-delete flag.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC deletion timestamp.</summary>
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Marker interface for auditable entities not inheriting <see cref="AuditableEntity"/>.
/// </summary>
public interface IAuditableEntityMarker
{
    /// <summary>Creation timestamp (UTC).</summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>Creator identifier.</summary>
    string? CreatedBy { get; set; }

    /// <summary>Update timestamp (UTC).</summary>
    DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Updater identifier.</summary>
    string? UpdatedBy { get; set; }

    /// <summary>Soft-delete flag.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Deletion timestamp (UTC).</summary>
    DateTime? DeletedAtUtc { get; set; }
}

/// <summary>
/// Application role for ASP.NET Core Identity.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>Initializes a new role.</summary>
    public ApplicationRole()
    {
    }

    /// <summary>Initializes a new role with a name.</summary>
    /// <param name="roleName">Role name.</param>
    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}
