using MediatR;

namespace ClinicBook.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>UTC time the event occurred.</summary>
    DateTime OccurredOnUtc { get; }
}
