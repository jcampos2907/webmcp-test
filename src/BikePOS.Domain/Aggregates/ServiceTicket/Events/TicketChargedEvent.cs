using BikePOS.Domain.Events;

namespace BikePOS.Domain.Aggregates.ServiceTicket.Events;

public record TicketChargedEvent(
    string TicketId,
    string ChargeId,
    decimal Amount,
    string? CashierName,
    string? StoreId
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
