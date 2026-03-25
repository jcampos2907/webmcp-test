using BikePOS.Domain.Events;

namespace BikePOS.Domain.Aggregates.Billing.Events;

public record ChargeRefundedEvent(
    string ChargeId,
    string TicketId,
    decimal Amount,
    string? RefundedBy
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
