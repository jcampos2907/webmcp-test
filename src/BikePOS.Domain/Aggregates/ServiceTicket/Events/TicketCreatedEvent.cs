using BikePOS.Domain.Events;

namespace BikePOS.Domain.Aggregates.ServiceTicket.Events;

public record TicketCreatedEvent(
    string TicketId,
    string ComponentId,
    string? CustomerId,
    string? MechanicId,
    string? StoreId
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
