using BikePOS.Domain.Aggregates.ServiceTicket.Events;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Infrastructure.Erp;
using BikePOS.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;

namespace BikePOS.Application.EventHandlers;

/// <summary>
/// When a ticket is fully charged, notify the customer (if configured)
/// and trigger ERP sync.
/// </summary>
public class TicketChargedEventHandler : IDomainEventHandler<TicketChargedEvent>
{
    private readonly NotificationService _notificationService;
    private readonly SyncTriggerService _syncTrigger;
    private readonly ILogger<TicketChargedEventHandler> _logger;

    public TicketChargedEventHandler(
        NotificationService notificationService,
        SyncTriggerService syncTrigger,
        ILogger<TicketChargedEventHandler> logger)
    {
        _notificationService = notificationService;
        _syncTrigger = syncTrigger;
        _logger = logger;
    }

    public async Task HandleAsync(TicketChargedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Ticket {TicketId} charged by {Cashier} for {Amount}",
            domainEvent.TicketId, domainEvent.CashierName, domainEvent.Amount);

        // Trigger ERP sync for the charge
        _syncTrigger.TriggerPush("Charge", domainEvent.ChargeId);

        // Customer notification could be triggered here in the future
        // await _notificationService.NotifyTicketReadyAsync(domainEvent.TicketId);
    }
}
