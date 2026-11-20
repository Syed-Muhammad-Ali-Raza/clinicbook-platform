using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Domain.Entities;
using ClinicBook.Infrastructure.Persistence;

namespace ClinicBook.Infrastructure.Auditing;

/// <summary>
/// Persists audit log entries.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _dbContext;
    private readonly IClock _clock;

    /// <summary>Initializes a new instance of the <see cref="AuditService"/> class.</summary>
    public AuditService(AppDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task LogAsync(
        string action,
        string outcome = "Success",
        Guid? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? traceId = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            OccurredAtUtc = _clock.UtcNow,
            UserId = userId?.ToString(),
            Action = action,
            Outcome = outcome,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            TraceId = traceId
        };

        _dbContext.AuditLogs.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
