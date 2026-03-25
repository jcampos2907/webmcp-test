using BikePOS.Domain.Aggregates.ServiceTicket;
using BikePOS.Domain.Aggregates.ServiceTicket.Events;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;

namespace BikePOS.Application.EventHandlers;

/// <summary>
/// Reacts to ticket status changes — triggers notifications when ticket is completed.
/// </summary>
public class TicketStatusChangedEventHandler : IDomainEventHandler<TicketStatusChangedEvent>
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<TicketStatusChangedEventHandler> _logger;

    public TicketStatusChangedEventHandler(
        NotificationService notificationService,
        ILogger<TicketStatusChangedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(TicketStatusChangedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Ticket {TicketId} status: {Old} → {New} by {User}",
            domainEvent.TicketId, domainEvent.OldStatus, domainEvent.NewStatus, domainEvent.ChangedBy);

        // Notify customer when ticket is completed (ready for pickup)
        if (domainEvent.NewStatus == TicketStatus.Completed)
        {
            try
            {
                await _notificationService.NotifyTicketReadyAsync(domainEvent.TicketId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification for ticket {TicketId}", domainEvent.TicketId);
            }
        }
    }
}
