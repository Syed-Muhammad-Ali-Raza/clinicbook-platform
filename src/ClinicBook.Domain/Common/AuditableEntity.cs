namespace ClinicBook.Domain.Common;

/// <summary>
/// Base type for entities that require auditing and soft deletion.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>UTC timestamp when the entity was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Identifier of the creator.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>UTC timestamp when the entity was last updated.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Identifier of the last updater.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Indicates whether the entity is soft deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the entity was soft deleted.</summary>
    public DateTime? DeletedAtUtc { get; set; }
}
