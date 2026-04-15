using BikePOS.Data;
using BikePOS.Domain.Aggregates.ServiceTicket;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Services;
using BikePOS.Infrastructure.Erp;
using Microsoft.EntityFrameworkCore;
using DomainPaymentMethod = BikePOS.Domain.Aggregates.ServiceTicket.PaymentMethod;

namespace BikePOS.Application.Commands;

public record CancelTicketRequest(
    string TicketId,
    string? CancelledBy,
    string? StoreId
);

public class CancelTicketCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly TicketEventService _ticketEventService;
    private readonly SyncTriggerService _syncTrigger;
    private readonly PermissionGuard _guard;

    public CancelTicketCommandHandler(
        IDbContextFactory<BikePosContext> dbFactory,
        IDomainEventDispatcher eventDispatcher,
        TicketEventService ticketEventService,
        SyncTriggerService syncTrigger,
        PermissionGuard guard)
    {
        _dbFactory = dbFactory;
        _eventDispatcher = eventDispatcher;
        _ticketEventService = ticketEventService;
        _syncTrigger = syncTrigger;
        _guard = guard;
    }

    public async Task<bool> HandleAsync(CancelTicketRequest request, CancellationToken ct = default)
    {
        _guard.Require("tickets.manage");
        using var db = _dbFactory.CreateDbContext();

        var ticket = await db.ServiceTicket
            .Include(t => t.BaseService)
            .Include(t => t.TicketProducts).ThenInclude(tp => tp.Product)
            .Include(t => t.Charges)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);

        if (ticket == null) return false;

        // Reconstitute aggregate
        var aggregate = ReconstructAggregate(ticket);

        // Cancel via domain logic — returns inventory to restore
        List<(string ProductId, int Quantity)> inventoryToRestore;
        try
        {
            inventoryToRestore = aggregate.Cancel(request.CancelledBy);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        // Restore inventory
        foreach (var (productId, quantity) in inventoryToRestore)
        {
            var product = await db.Product.FindAsync(new object[] { productId }, ct);
            if (product != null)
            {
                product.QuantityInStock += quantity;
            }
        }

        // Update persistence
        ticket.Status = Models.TicketStatus.Cancelled;
        ticket.UpdatedAt = aggregate.UpdatedAt;
        ticket.UpdatedBy = request.CancelledBy;

        await db.SaveChangesAsync(ct);

        // Dispatch domain events
        await _eventDispatcher.DispatchAsync(aggregate.DomainEvents, ct);
        aggregate.ClearDomainEvents();

        // Timeline
        await _ticketEventService.RecordCancellation(ticket.Id, request.CancelledBy, request.StoreId);

        // ERP sync
        _syncTrigger.TriggerPush("ServiceTicket", ticket.Id);

        return true;
    }

    private static ServiceTicketAggregate ReconstructAggregate(Models.ServiceTicket ticket)
    {
        var lineItems = ticket.TicketProducts.Select(tp =>
            new LineItem(tp.ProductId, tp.Product?.Name ?? "", tp.UnitPrice, tp.Quantity)).ToList();

        var charges = ticket.Charges.Select(c =>
        {
            var cr = new ChargeRecord(
                c.Id, c.ServiceTicketId, c.Amount,
                (DomainPaymentMethod)(int)c.PaymentMethod,
                c.CashierName, c.StoreId);
            if (c.PaymentStatus == Models.PaymentStatus.Completed) cr.MarkCompleted();
            else if (c.PaymentStatus == Models.PaymentStatus.Failed) cr.MarkFailed();
            else if (c.PaymentStatus == Models.PaymentStatus.Cancelled) cr.MarkCancelled();
            return cr;
        }).ToList();

        return ServiceTicketAggregate.Reconstitute(
            ticket.Id, ticket.TicketNumber,
            (TicketStatus)(int)ticket.Status,
            ticket.ComponentId, ticket.CustomerId, ticket.MechanicId,
            ticket.BaseServiceId, ticket.BaseService?.DefaultPrice ?? ticket.Price,
            ticket.Description, ticket.DiscountPercent,
            ticket.StoreId, ticket.CreatedBy, ticket.UpdatedBy,
            ticket.CreatedAt, ticket.UpdatedAt,
            lineItems, charges);
    }
}
