using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Queries;

public record TicketListItemDto(
    string Id, string TicketDisplay, int TicketNumber, TicketStatus Status,
    string? ComponentName, string? ComponentType, string? CustomerName,
    string? MechanicName, decimal Price, DateTime CreatedAt);

public class ListTicketsQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public ListTicketsQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<ServiceTicket>> HandleAsync(TicketStatus? statusFilter = null, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.ServiceTicket
            .Include(t => t.Component)
            .Include(t => t.Customer)
            .Include(t => t.Mechanic)
            .Include(t => t.BaseService)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(t => t.Status == statusFilter.Value);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }
}

public class GetTicketByIdQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public GetTicketByIdQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<ServiceTicket?> HandleAsync(string id, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.ServiceTicket
            .Include(t => t.Component)
            .Include(t => t.Customer)
            .Include(t => t.Mechanic)
            .Include(t => t.BaseService)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}

public class SearchTicketsQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public SearchTicketsQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<ServiceTicket>> HandleAsync(string query, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var term = query.Trim().ToLower();

        var activeStatuses = new[] { TicketStatus.Open, TicketStatus.InProgress, TicketStatus.WaitingForParts, TicketStatus.Completed };

        return await db.ServiceTicket
            .Include(t => t.Component)
            .Include(t => t.Customer)
            .Include(t => t.BaseService)
            .Where(t => activeStatuses.Contains(t.Status) &&
                (t.TicketNumber.ToString().Contains(term) ||
                 (t.Component != null && t.Component.Name.ToLower().Contains(term)) ||
                 (t.Customer != null && (t.Customer.FirstName.ToLower().Contains(term) || t.Customer.LastName.ToLower().Contains(term)))))
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .ToListAsync(ct);
    }
}
