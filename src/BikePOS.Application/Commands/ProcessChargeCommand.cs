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

public record ProcessChargeRequest(
    string TicketId,
    decimal Amount,
    DomainPaymentMethod PaymentMethod,
    string? CashierName,
    string? StoreId
);

public record ProcessChargeResult(
    string ChargeId,
    decimal ChargedAmount,
    bool IsFullyPaid,
    string? ErrorMessage
);

public class ProcessChargeCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly TicketEventService _ticketEventService;
    private readonly SyncTriggerService _syncTrigger;

    public ProcessChargeCommandHandler(
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

    public async Task<ProcessChargeResult> HandleAsync(ProcessChargeRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();

        var ticket = await db.ServiceTicket
            .Include(t => t.BaseService)
            .Include(t => t.TicketProducts).ThenInclude(tp => tp.Product)
            .Include(t => t.Charges)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);

        if (ticket == null)
            return new ProcessChargeResult("", 0, false, "Ticket not found.");

        // Reconstitute domain aggregate
        var aggregate = ReconstructAggregate(ticket);

        // Process charge via domain logic
        ChargeRecord chargeRecord;
        try
        {
            chargeRecord = aggregate.ProcessCharge(
                request.Amount,
                request.PaymentMethod,
                request.CashierName,
                request.StoreId);
        }
        catch (InvalidOperationException ex)
        {
            return new ProcessChargeResult("", 0, false, ex.Message);
        }

        // Persist charge
        var charge = new Charge
        {
            Id = chargeRecord.Id,
            ServiceTicketId = ticket.Id,
            Amount = chargeRecord.Amount,
            PaymentMethod = (Models.PaymentMethod)(int)chargeRecord.PaymentMethod,
            PaymentStatus = (Models.PaymentStatus)(int)chargeRecord.PaymentStatus,
            CashierName = chargeRecord.CashierName,
            ChargedAt = chargeRecord.ChargedAt,
            CompletedAt = chargeRecord.CompletedAt,
            Notes = chargeRecord.Notes,
            StoreId = request.StoreId,
            CreatedBy = request.CashierName
        };
        db.Charge.Add(charge);

        // Update ticket status if changed
        ticket.Status = (Models.TicketStatus)(int)aggregate.Status;
        ticket.Price = aggregate.Total;
        ticket.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // Dispatch domain events
        await _eventDispatcher.DispatchAsync(aggregate.DomainEvents, ct);
        aggregate.ClearDomainEvents();

        // Timeline events
        await _ticketEventService.RecordCharge(
            ticket.Id, charge.Amount, charge.PaymentMethod.ToString(),
            request.CashierName, request.StoreId);

        if (aggregate.IsFullyPaid)
        {
            await _ticketEventService.RecordStatusChange(
                ticket.Id, Models.TicketStatus.Completed, Models.TicketStatus.Charged,
                request.CashierName, request.StoreId);
        }

        // ERP sync
        _syncTrigger.TriggerPush("Charge", charge.Id);
        _syncTrigger.TriggerPush("ServiceTicket", ticket.Id);

        return new ProcessChargeResult(charge.Id, charge.Amount, aggregate.IsFullyPaid, null);
    }

    private static ServiceTicketAggregate ReconstructAggregate(ServiceTicket ticket)
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
            ticket.Id,
            ticket.TicketNumber,
            (DomainTicketStatus)(int)ticket.Status,
            ticket.ComponentId,
            ticket.CustomerId,
            ticket.MechanicId,
            ticket.BaseServiceId,
            ticket.BaseService?.DefaultPrice ?? ticket.Price,
            ticket.Description,
            ticket.DiscountPercent,
            ticket.StoreId,
            ticket.CreatedBy,
            ticket.UpdatedBy,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            lineItems,
            charges);
    }
}
