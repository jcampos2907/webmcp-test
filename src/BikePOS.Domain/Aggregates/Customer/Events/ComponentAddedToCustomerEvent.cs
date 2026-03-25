using BikePOS.Domain.Events;

namespace BikePOS.Domain.Aggregates.Customer.Events;

public record ComponentAddedToCustomerEvent(
    string CustomerId,
    string ComponentId,
    string ComponentType
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
