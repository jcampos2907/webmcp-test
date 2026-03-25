using BikePOS.Data;
using BikePOS.Domain.Aggregates.ServiceTicket;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Interfaces.Repositories;
using BikePOS.Models;
using BikePOS.Services;
using BikePOS.Infrastructure.Erp;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Commands;

public record CreateTicketRequest(
    string ComponentId,
    string? CustomerId,
    string? MechanicId,
    string? BaseServiceId,
    decimal BaseServicePrice,
    string? Description,
    decimal DiscountPercent,
    string? StoreId,
    string? CreatedBy,
    List<ProductLineRequest> Products
);

public record ProductLineRequest(string ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CreateTicketResult(string TicketId, int TicketNumber, string TicketDisplay);

public class CreateTicketCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly TicketEventService _ticketEventService;
    private readonly SyncTriggerService _syncTrigger;

    public CreateTicketCommandHandler(
        IDbContextFactory<BikePosContext> dbFactory,
        IDomainEventDispatcher eventDispatcher,
        TicketEventService ticketEventService,
        SyncTriggerService syncTrigger)
    {
        _dbFactory = dbFactory;
        _eventDispatcher = eventDispatcher;
        _ticketEventService = ticketEventService;
        _syncTrigger = syncTrigger;
    }

    public async Task<CreateTicketResult> HandleAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();

        // Get next ticket number
        var maxTicketNumber = await db.ServiceTicket
            .Where(t => t.StoreId == request.StoreId)
            .MaxAsync(t => (int?)t.TicketNumber, ct) ?? 0;

        // Create domain aggregate
        var aggregate = ServiceTicketAggregate.Create(
            request.ComponentId,
            request.CustomerId,
            request.MechanicId,
            request.BaseServiceId,
            request.BaseServicePrice,
            request.Description,
            request.DiscountPercent,
            maxTicketNumber + 1,
            request.StoreId,
            request.CreatedBy);

        // Add products
        foreach (var p in request.Products)
        {
            aggregate.AddProduct(p.ProductId, p.ProductName, p.UnitPrice, p.Quantity, request.CreatedBy);
        }

        // Map to persistence model
        var ticket = new ServiceTicket
        {
            Id = aggregate.Id,
            TicketNumber = aggregate.TicketNumber,
            Status = (Models.TicketStatus)(int)aggregate.Status,
            ComponentId = aggregate.ComponentId,
            CustomerId = aggregate.CustomerId,
            MechanicId = aggregate.MechanicId,
            BaseServiceId = aggregate.BaseServiceId,
            Description = aggregate.Description,
            Price = aggregate.Total,
            DiscountPercent = aggregate.DiscountPercent,
            StoreId = aggregate.StoreId,
            CreatedBy = aggregate.CreatedBy,
            UpdatedBy = aggregate.UpdatedBy,
            CreatedAt = aggregate.CreatedAt,
            UpdatedAt = aggregate.UpdatedAt
        };

        db.ServiceTicket.Add(ticket);

        // Add ticket products and decrement inventory
        foreach (var line in aggregate.LineItems)
        {
            db.TicketProduct.Add(new TicketProduct
            {
                ServiceTicketId = ticket.Id,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            });

            var product = await db.Product.FindAsync(new object[] { line.ProductId }, ct);
            if (product != null)
            {
                product.QuantityInStock -= line.Quantity;
            }
        }

        await db.SaveChangesAsync(ct);

        // Dispatch domain events
        await _eventDispatcher.DispatchAsync(aggregate.DomainEvents, ct);
        aggregate.ClearDomainEvents();

        // Record timeline events (existing pattern compatibility)
        await _ticketEventService.RecordCreated(ticket.Id, ticket.TicketDisplay, request.CreatedBy, request.StoreId);
        foreach (var line in aggregate.LineItems)
        {
            await _ticketEventService.RecordProductAdded(ticket.Id, line.ProductName, line.Quantity, request.CreatedBy, request.StoreId);
        }
        if (request.MechanicId != null)
        {
            var mechanic = await db.Mechanic.FindAsync(new object[] { request.MechanicId }, ct);
            await _ticketEventService.RecordMechanicAssigned(ticket.Id, mechanic?.Name, request.CreatedBy, request.StoreId);
        }

        // ERP sync
        _syncTrigger.TriggerPush("ServiceTicket", ticket.Id);

        return new CreateTicketResult(ticket.Id, ticket.TicketNumber, ticket.TicketDisplay);
    }
}
