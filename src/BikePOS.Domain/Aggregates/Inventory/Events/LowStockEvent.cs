using BikePOS.Domain.Events;

namespace BikePOS.Domain.Aggregates.Inventory.Events;

public record LowStockEvent(
    string ProductId,
    string ProductName,
    int RemainingStock,
    string? StoreId
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
