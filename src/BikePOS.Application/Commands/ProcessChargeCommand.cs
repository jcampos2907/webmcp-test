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
    string? StoreId,
    string? TerminalId = null
);

public record ProcessChargeResult(
    string ChargeId,
    decimal ChargedAmount,
    bool IsFullyPaid,
    string? ErrorMessage,
    string? PaymentSessionId = null
);

public class ProcessChargeCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly TicketEventService _ticketEventService;
    private readonly SyncTriggerService _syncTrigger;
    private readonly Infrastructure.Payments.PaymentTerminalService _terminalService;

    public ProcessChargeCommandHandler(
        IDbContextFactory<BikePosContext> dbFactory,
        IDomainEventDispatcher eventDispatcher,
        TicketEventService ticketEventService,
        SyncTriggerService syncTrigger,
        Infrastructure.Payments.PaymentTerminalService terminalService)
    {
        _dbFactory = dbFactory;
        _eventDispatcher = eventDispatcher;
        _ticketEventService = ticketEventService;
        _syncTrigger = syncTrigger;
        _terminalService = terminalService;
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

        // For Card payments with a terminal, initiate terminal payment session
        string? paymentSessionId = null;
        if (request.PaymentMethod == DomainPaymentMethod.Card && request.TerminalId is not null)
        {
            var terminal = await db.PaymentTerminal.FindAsync(new object[] { request.TerminalId }, ct);
            if (terminal is null)
                return new ProcessChargeResult("", 0, false, "Terminal not found.");

            var provider = _terminalService.GetProvider(terminal);
            var paymentRequest = new Interfaces.Services.PaymentRequest(request.Amount, "CRC", ticket.TicketNumber.ToString());
            var session = await provider.CreatePaymentAsync(terminal, paymentRequest);

            session.ChargeId = charge.Id;
            db.PaymentSession.Add(session);
            paymentSessionId = session.Id;
        }

        // Update ticket status if changed (only for completed charges)
        ticket.Status = (Models.TicketStatus)(int)aggregate.Status;
        ticket.Price = aggregate.Total;
        ticket.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // For pending card payments, return early — completion happens in ConfirmTerminalPayment
        if (chargeRecord.PaymentStatus == Domain.Aggregates.ServiceTicket.PaymentStatus.Pending)
        {
            return new ProcessChargeResult(charge.Id, charge.Amount, false, null, paymentSessionId);
        }

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

    public async Task<PaymentSessionStatus> PollTerminalStatusAsync(string sessionId, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var session = await db.PaymentSession
            .Include(s => s.Terminal)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null) return PaymentSessionStatus.Failed;
        if (session.Status is PaymentSessionStatus.Completed or PaymentSessionStatus.Failed or PaymentSessionStatus.Cancelled)
            return session.Status;

        var provider = _terminalService.GetProvider(session.Terminal);
        var status = await provider.GetStatusAsync(session.Terminal, session.ExternalRef!);

        if (status != session.Status)
        {
            session.Status = status;
            if (status is PaymentSessionStatus.Completed or PaymentSessionStatus.Failed or PaymentSessionStatus.Cancelled)
                session.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return status;
    }

    public async Task<ProcessChargeResult> ConfirmTerminalPaymentAsync(string sessionId, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var session = await db.PaymentSession
            .Include(s => s.Terminal)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return new ProcessChargeResult("", 0, false, "Payment session not found.");

        var charge = await db.Charge.FindAsync(new object[] { session.ChargeId }, ct);
        if (charge is null)
            return new ProcessChargeResult("", 0, false, "Charge not found.");

        charge.PaymentStatus = Models.PaymentStatus.Completed;
        charge.CompletedAt = DateTime.UtcNow;
        charge.ExternalTransactionId = session.ExternalRef;

        var ticket = await db.ServiceTicket
            .Include(t => t.BaseService)
            .Include(t => t.TicketProducts).ThenInclude(tp => tp.Product)
            .Include(t => t.Charges)
            .FirstOrDefaultAsync(t => t.Id == charge.ServiceTicketId, ct);

        if (ticket is null)
            return new ProcessChargeResult("", 0, false, "Ticket not found.");

        var aggregate = ReconstructAggregate(ticket);
        var isFullyPaid = aggregate.IsFullyPaid;

        if (isFullyPaid)
        {
            ticket.Status = Models.TicketStatus.Charged;
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        if (isFullyPaid)
        {
            await _eventDispatcher.DispatchAsync(aggregate.DomainEvents, ct);
            aggregate.ClearDomainEvents();

            await _ticketEventService.RecordStatusChange(
                ticket.Id, Models.TicketStatus.Completed, Models.TicketStatus.Charged,
                charge.CashierName, charge.StoreId);
        }

        await _ticketEventService.RecordCharge(
            ticket.Id, charge.Amount, charge.PaymentMethod.ToString(),
            charge.CashierName, charge.StoreId);

        _syncTrigger.TriggerPush("Charge", charge.Id);
        _syncTrigger.TriggerPush("ServiceTicket", ticket.Id);

        return new ProcessChargeResult(charge.Id, charge.Amount, isFullyPaid, null);
    }

    public async Task<bool> CancelTerminalPaymentAsync(string sessionId, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var session = await db.PaymentSession
            .Include(s => s.Terminal)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null) return false;

        var provider = _terminalService.GetProvider(session.Terminal);
        var cancelled = await provider.CancelAsync(session.Terminal, session.ExternalRef!);

        if (cancelled)
        {
            session.Status = PaymentSessionStatus.Cancelled;
            session.CompletedAt = DateTime.UtcNow;

            var charge = await db.Charge.FindAsync(new object[] { session.ChargeId }, ct);
            if (charge is not null)
            {
                charge.PaymentStatus = Models.PaymentStatus.Cancelled;
                charge.CompletedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
        }

        return cancelled;
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
