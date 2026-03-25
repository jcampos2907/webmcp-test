using BikePOS.Domain.Events;

namespace BikePOS.Interfaces.Events;

/// <summary>
/// Handles a specific domain event. Multiple handlers can be registered for the same event type.
/// Handlers run after persistence — they should be idempotent and tolerant of failures.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
