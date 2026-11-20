using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ClinicBook.Infrastructure.Persistence;

/// <summary>
/// Application database context. Entity configurations and sets are added in later phases.
/// </summary>
public sealed class AppDbContext : DbContext, IUnitOfWorkMarker
{
    private readonly IClock _clock;

    /// <summary>Initializes a new instance of the <see cref="AppDbContext"/> class.</summary>
    /// <param name="options">DbContext options.</param>
    /// <param name="clock">System clock abstraction.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options, IClock clock) : base(options)
    {
        _clock = clock;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
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
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    break;
                case EntityState.Deleted:
                    // Soft-delete is enforced via interceptors/config in later phases.
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
