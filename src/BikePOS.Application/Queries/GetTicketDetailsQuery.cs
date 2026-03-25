using BikePOS.Application.DTOs;
using BikePOS.Data;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Queries;

public class GetTicketDetailsQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public GetTicketDetailsQueryHandler(IDbContextFactory<BikePosContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<TicketDetailsDto?> HandleAsync(string ticketId, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();

        var ticket = await db.ServiceTicket
            .Include(t => t.Component)
            .Include(t => t.Customer)
            .Include(t => t.Mechanic)
            .Include(t => t.BaseService)
            .Include(t => t.TicketProducts).ThenInclude(tp => tp.Product)
            .Include(t => t.Charges)
            .Include(t => t.Events.OrderBy(e => e.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket == null) return null;

        var completedCharges = ticket.Charges
            .Where(c => c.PaymentStatus == Models.PaymentStatus.Completed)
            .Sum(c => c.Amount);

        return new TicketDetailsDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            TicketDisplay = ticket.TicketDisplay,
            Status = ticket.Status.ToString(),
            ComponentName = ticket.Component?.Name,
            ComponentType = ticket.Component?.ComponentType,
            CustomerName = ticket.Customer?.FullName,
            MechanicName = ticket.Mechanic?.Name,
            ServiceName = ticket.BaseService?.Name,
            ServicePrice = ticket.BaseService?.DefaultPrice ?? 0,
            Description = ticket.Description,
            DiscountPercent = ticket.DiscountPercent,
            Subtotal = (ticket.BaseService?.DefaultPrice ?? 0)
                       + ticket.TicketProducts.Sum(tp => tp.UnitPrice * tp.Quantity),
            Total = ticket.Price,
            TotalCharged = completedCharges,
            RemainingBalance = ticket.Price - completedCharges,
            IsFullyPaid = completedCharges >= ticket.Price && ticket.Price > 0,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            CreatedBy = ticket.CreatedBy,
            Products = ticket.TicketProducts.Select(tp => new TicketProductDto
            {
                ProductId = tp.ProductId,
                ProductName = tp.Product?.Name ?? "",
                Quantity = tp.Quantity,
                UnitPrice = tp.UnitPrice,
                LineTotal = tp.Quantity * tp.UnitPrice
            }).ToList(),
            Charges = ticket.Charges.Select(c => new ChargeDto
            {
                Id = c.Id,
                Amount = c.Amount,
                PaymentMethod = c.PaymentMethod.ToString(),
                PaymentStatus = c.PaymentStatus.ToString(),
                CashierName = c.CashierName,
                ChargedAt = c.ChargedAt,
                Notes = c.Notes
            }).OrderByDescending(c => c.ChargedAt).ToList(),
            Events = ticket.Events.Select(e => new TicketEventDto
            {
                EventType = e.EventType.ToString(),
                Description = e.Description,
                CreatedAt = e.CreatedAt,
                CreatedBy = e.CreatedBy
            }).ToList()
        };
    }
}
