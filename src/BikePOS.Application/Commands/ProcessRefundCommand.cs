using BikePOS.Data;
using BikePOS.Domain.Aggregates.ServiceTicket;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Models;
using BikePOS.Services;
using BikePOS.Infrastructure.Erp;
using Microsoft.EntityFrameworkCore;
using DomainPaymentMethod = BikePOS.Domain.Aggregates.ServiceTicket.PaymentMethod;
using DomainTicketStatus = BikePOS.Domain.Aggregates.ServiceTicket.TicketStatus;

namespace BikePOS.Application.Commands;

public record ProcessRefundRequest(
    string TicketId,
    decimal Amount,
    DomainPaymentMethod PaymentMethod,
    string? CashierName,
    string? StoreId
);

public record ProcessRefundResult(
    string ChargeId,
    decimal RefundedAmount,
    string? ErrorMessage
);

public class ProcessRefundCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly TicketEventService _ticketEventService;
    private readonly SyncTriggerService _syncTrigger;
    private readonly PermissionGuard _guard;

    public ProcessRefundCommandHandler(
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

    public async Task<ProcessRefundResult> HandleAsync(ProcessRefundRequest request, CancellationToken ct = default)
    {
        _guard.Require("tickets.manage");
        using var db = _dbFactory.CreateDbContext();

        var ticket = await db.ServiceTicket
            .Include(t => t.BaseService)
            .Include(t => t.TicketProducts).ThenInclude(tp => tp.Product)
            .Include(t => t.Charges)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);

        if (ticket == null)
            return new ProcessRefundResult("", 0, "Ticket not found.");

        var aggregate = ReconstructAggregate(ticket);

        ChargeRecord refundRecord;
        try
        {
            refundRecord = aggregate.ProcessRefund(
                request.Amount,
                request.PaymentMethod,
                request.CashierName,
                request.StoreId);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return new ProcessRefundResult("", 0, ex.Message);
        }

        // Persist refund charge (negative amount)
        var charge = new Charge
        {
            Id = refundRecord.Id,
            ServiceTicketId = ticket.Id,
            Amount = refundRecord.Amount, // negative
            PaymentMethod = (Models.PaymentMethod)(int)refundRecord.PaymentMethod,
            PaymentStatus = (Models.PaymentStatus)(int)refundRecord.PaymentStatus,
            CashierName = refundRecord.CashierName,
            ChargedAt = refundRecord.ChargedAt,
            CompletedAt = refundRecord.CompletedAt,
            Notes = refundRecord.Notes,
            StoreId = request.StoreId,
            CreatedBy = request.CashierName
        };
        db.Charge.Add(charge);

        // Update ticket status
        var oldStatus = ticket.Status;
        ticket.Status = (Models.TicketStatus)(int)aggregate.Status;
        ticket.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // Dispatch domain events
        await _eventDispatcher.DispatchAsync(aggregate.DomainEvents, ct);
        aggregate.ClearDomainEvents();

        // Timeline
        await _ticketEventService.RecordRefund(
            ticket.Id, request.Amount, request.PaymentMethod.ToString(),
            request.CashierName, request.StoreId);

        if (ticket.Status != oldStatus)
        {
            await _ticketEventService.RecordStatusChange(
                ticket.Id, oldStatus, ticket.Status,
                request.CashierName, request.StoreId);
        }

        // ERP sync
        _syncTrigger.TriggerPush("Charge", charge.Id);
        _syncTrigger.TriggerPush("ServiceTicket", ticket.Id);

        return new ProcessRefundResult(charge.Id, Math.Abs(refundRecord.Amount), null);
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
            (DomainTicketStatus)(int)ticket.Status,
            ticket.ComponentId, ticket.CustomerId, ticket.MechanicId,
            ticket.BaseServiceId, ticket.BaseService?.DefaultPrice ?? ticket.Price,
            ticket.Description, ticket.DiscountPercent,
            ticket.StoreId, ticket.CreatedBy, ticket.UpdatedBy,
            ticket.CreatedAt, ticket.UpdatedAt,
            lineItems, charges);
    }
}
