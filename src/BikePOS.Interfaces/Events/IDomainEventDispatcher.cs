using BikePOS.Domain.Events;

namespace BikePOS.Interfaces.Events;

/// <summary>
/// Dispatches domain events to their handlers after the aggregate is persisted.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
