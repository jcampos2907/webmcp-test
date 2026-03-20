using Microsoft.EntityFrameworkCore;
using BikePOS.Data;
using BikePOS.Models;

namespace BikePOS.Services;

public class TicketEventService
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public TicketEventService(IDbContextFactory<BikePosContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task RecordAsync(string ticketId, TicketEventType type, string? description, string? details, string? createdBy, string? storeId)
    {
        using var context = _dbFactory.CreateDbContext();
        context.TicketEvent.Add(new TicketEvent
        {
            ServiceTicketId = ticketId,
            EventType = type,
            Description = description,
            Details = details,
            CreatedBy = createdBy,
            StoreId = storeId,
        });
        await context.SaveChangesAsync();
    }

    public Task RecordCreated(string ticketId, string ticketDisplay, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.Created, ticketDisplay, null, createdBy, storeId);

    public Task RecordStatusChange(string ticketId, TicketStatus oldStatus, TicketStatus newStatus, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.StatusChanged, $"{oldStatus} → {newStatus}", null, createdBy, storeId);

    public Task RecordMechanicAssigned(string ticketId, string? mechanicName, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.MechanicAssigned, mechanicName, null, createdBy, storeId);

    public Task RecordProductAdded(string ticketId, string productName, int quantity, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.ProductAdded, $"{productName} x{quantity}", null, createdBy, storeId);

    public Task RecordProductRemoved(string ticketId, string productName, int quantity, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.ProductRemoved, $"{productName} x{quantity}", null, createdBy, storeId);

    public Task RecordNoteUpdated(string ticketId, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.NoteUpdated, null, null, createdBy, storeId);

    public Task RecordDiscountChanged(string ticketId, decimal oldDiscount, decimal newDiscount, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.DiscountChanged, $"{oldDiscount}% → {newDiscount}%", null, createdBy, storeId);

    public Task RecordCharge(string ticketId, decimal amount, string? paymentMethod, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.ChargeProcessed, $"{amount:C} ({paymentMethod})", null, createdBy, storeId);

    public Task RecordCancellation(string ticketId, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.Cancelled, null, null, createdBy, storeId);

    public Task RecordMetaFieldChanged(string ticketId, string fieldLabel, string? createdBy, string? storeId) =>
        RecordAsync(ticketId, TicketEventType.MetaFieldChanged, fieldLabel, null, createdBy, storeId);
}
