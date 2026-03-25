using BikePOS.Domain.Events;

namespace BikePOS.Domain.Aggregates.Customer.Events;

public record CustomerCreatedEvent(
    string CustomerId,
    string FirstName,
    string LastName,
    string? StoreId
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
