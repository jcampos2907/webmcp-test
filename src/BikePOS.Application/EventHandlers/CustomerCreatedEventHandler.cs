using BikePOS.Domain.Aggregates.Customer.Events;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Infrastructure.Erp;
using Microsoft.Extensions.Logging;

namespace BikePOS.Application.EventHandlers;

/// <summary>
/// When a customer is created, trigger ERP sync.
/// </summary>
public class CustomerCreatedEventHandler : IDomainEventHandler<CustomerCreatedEvent>
{
    private readonly SyncTriggerService _syncTrigger;
    private readonly ILogger<CustomerCreatedEventHandler> _logger;

    public CustomerCreatedEventHandler(SyncTriggerService syncTrigger, ILogger<CustomerCreatedEventHandler> logger)
    {
        _syncTrigger = syncTrigger;
        _logger = logger;
    }

    public async Task HandleAsync(CustomerCreatedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Customer created: {FirstName} {LastName} (ID: {CustomerId})",
            domainEvent.FirstName, domainEvent.LastName, domainEvent.CustomerId);

        _syncTrigger.TriggerPush("Customer", domainEvent.CustomerId);

        await Task.CompletedTask;
    }
}
