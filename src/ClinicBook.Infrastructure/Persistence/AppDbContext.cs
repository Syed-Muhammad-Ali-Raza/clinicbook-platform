using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Domain.Common;
using ClinicBook.Domain.Entities;
using ClinicBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicBook.Infrastructure.Persistence;

/// <summary>
/// Application database context including Identity and core security entities.
/// </summary>
public sealed class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IUnitOfWorkMarker
{
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="AppDbContext"/> class.</summary>
    /// <param name="options">DbContext options.</param>
    /// <param name="clock">System clock abstraction.</param>
    /// <param name="currentUser">Current user context.</param>
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IClock clock,
        ICurrentUser currentUser) : base(options)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    /// <summary>Refresh tokens issued to users.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Audit log entries.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.CreatedBy).HasMaxLength(256);
            entity.Property(x => x.UpdatedBy).HasMaxLength(256);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.ClinicId);
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.RevokedReason).HasMaxLength(256);
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(512);
            entity.Property(x => x.DeviceInfo).HasMaxLength(512);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.CreatedBy).HasMaxLength(256);
            entity.Property(x => x.UpdatedBy).HasMaxLength(256);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.FamilyId);
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(64);
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(128);
            entity.Property(x => x.EntityId).HasMaxLength(128);
            entity.Property(x => x.Outcome).HasMaxLength(32).IsRequired();
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
            entity.Property(x => x.TraceId).HasMaxLength(128);
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Action);
        });
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInformation()
    {
        var utcNow = _clock.UtcNow;
        var userId = _currentUser.UserId?.ToString();

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.CreatedBy ??= userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = utcNow;
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.CreatedBy ??= userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = utcNow;
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    break;
            }
        }
    }
}

/// <summary>
/// Marker interface to associate DbContext with the infrastructure unit of work.
/// </summary>
public interface IUnitOfWorkMarker
{
}
