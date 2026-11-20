#pragma warning disable CA1848
using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClinicBook.Infrastructure.Events;

/// <summary>
/// Dispatches domain events via MediatR.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventDispatcher> _logger;

    /// <summary>Initializes a new dispatcher.</summary>
    public DomainEventDispatcher(IPublisher publisher, ILogger<DomainEventDispatcher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogDebug("Dispatching domain event {EventType}", domainEvent.GetType().Name);
            if (domainEvent is INotification notification)
            {
                await _publisher.Publish(notification, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("Domain event {EventType} does not implement INotification and was not published", domainEvent.GetType().Name);
            }
        }
    }
}
#pragma warning restore CA1848
