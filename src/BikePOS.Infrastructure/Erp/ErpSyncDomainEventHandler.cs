using BikePOS.Domain.Aggregates.ServiceTicket.Events;
using BikePOS.Domain.Aggregates.Customer.Events;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Services;
using Microsoft.Extensions.Logging;

namespace BikePOS.Infrastructure.Erp;

/// <summary>
/// Listens to domain events and triggers ERP sync as a side effect.
/// This is the anti-corruption layer entry point — domain events drive sync
/// instead of manual SyncTriggerService calls scattered in UI code.
/// </summary>
public class ErpTicketCreatedHandler : IDomainEventHandler<TicketCreatedEvent>
{
    private readonly SyncTriggerService _syncTrigger;
    private readonly ILogger<ErpTicketCreatedHandler> _logger;

    public ErpTicketCreatedHandler(SyncTriggerService syncTrigger, ILogger<ErpTicketCreatedHandler> logger)
    {
        _syncTrigger = syncTrigger;
        _logger = logger;
    }

    public Task HandleAsync(TicketCreatedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogDebug("ERP sync: ticket created {TicketId}", domainEvent.TicketId);
        _syncTrigger.TriggerPush("ServiceTicket", domainEvent.TicketId);
        return Task.CompletedTask;
    }
}

public class ErpTicketStatusChangedHandler : IDomainEventHandler<TicketStatusChangedEvent>
{
    private readonly SyncTriggerService _syncTrigger;

    public ErpTicketStatusChangedHandler(SyncTriggerService syncTrigger)
    {
        _syncTrigger = syncTrigger;
    }

    public Task HandleAsync(TicketStatusChangedEvent domainEvent, CancellationToken ct = default)
    {
        _syncTrigger.TriggerPush("ServiceTicket", domainEvent.TicketId);
        return Task.CompletedTask;
    }
}

public class ErpCustomerCreatedHandler : IDomainEventHandler<CustomerCreatedEvent>
{
    private readonly SyncTriggerService _syncTrigger;

    public ErpCustomerCreatedHandler(SyncTriggerService syncTrigger)
    {
        _syncTrigger = syncTrigger;
    }

    public Task HandleAsync(CustomerCreatedEvent domainEvent, CancellationToken ct = default)
    {
        _syncTrigger.TriggerPush("Customer", domainEvent.CustomerId);
        return Task.CompletedTask;
    }
}
