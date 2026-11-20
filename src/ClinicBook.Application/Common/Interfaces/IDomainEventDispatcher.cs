using ClinicBook.Domain.Common;

namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Dispatches domain events after changes are persisted.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>Dispatches the provided domain events.</summary>
    /// <param name="domainEvents">Events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
