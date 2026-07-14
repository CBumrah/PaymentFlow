namespace PaymentFlow.SharedKernel.Primitives;

/// <summary>
/// Marker interface for domain events raised by aggregate roots.
/// Concrete events (e.g., PaymentCreatedEvent) will implement this
/// in PaymentFlow.Domain.
/// </summary>
public interface IDomainEvent
{
}

/// <summary>
/// Base class for aggregate roots — entities that are the entry point
/// for a consistency boundary (e.g., Payment). Aggregate roots collect
/// domain events raised during business operations so infrastructure
/// can dispatch them (e.g., write to the outbox) after a successful save.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot(Guid id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Called by infrastructure after events have been persisted/dispatched
    /// (typically after SaveChangesAsync) to avoid raising the same event twice.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}