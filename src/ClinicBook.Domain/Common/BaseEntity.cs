namespace ClinicBook.Domain.Common;

/// <summary>
/// Base type for all domain entities.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Unique identifier for the entity.</summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>Domain events raised by this entity.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Adds a domain event to be dispatched after persistence.</summary>
    /// <param name="domainEvent">The domain event.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Clears all pending domain events.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
