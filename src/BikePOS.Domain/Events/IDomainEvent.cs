namespace BikePOS.Domain.Events;

/// <summary>
/// Marker interface for domain events. Events represent something meaningful that happened
/// in the domain and are raised by aggregate roots.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
