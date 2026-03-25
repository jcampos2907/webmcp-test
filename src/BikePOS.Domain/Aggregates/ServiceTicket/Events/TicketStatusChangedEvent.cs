using BikePOS.Domain.Events;

namespace BikePOS.Domain.Aggregates.ServiceTicket.Events;

public record TicketStatusChangedEvent(
    string TicketId,
    TicketStatus OldStatus,
    TicketStatus NewStatus,
    string? ChangedBy
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
