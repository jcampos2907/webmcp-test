using BikePOS.Domain.Aggregates.Inventory.Events;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Infrastructure.Erp;
using Microsoft.Extensions.Logging;

namespace BikePOS.Application.EventHandlers;

/// <summary>
/// When a product hits low stock, trigger ERP sync to notify the external system.
/// </summary>
public class LowStockEventHandler : IDomainEventHandler<LowStockEvent>
{
    private readonly SyncTriggerService _syncTrigger;
    private readonly ILogger<LowStockEventHandler> _logger;

    public LowStockEventHandler(SyncTriggerService syncTrigger, ILogger<LowStockEventHandler> logger)
    {
        _syncTrigger = syncTrigger;
        _logger = logger;
    }

    public async Task HandleAsync(LowStockEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Low stock alert: {ProductName} (ID: {ProductId}) has {Remaining} units left",
            domainEvent.ProductName, domainEvent.ProductId, domainEvent.RemainingStock);

        // Sync updated inventory to ERP
        _syncTrigger.TriggerPush("Product", domainEvent.ProductId);

        await Task.CompletedTask;
    }
}
