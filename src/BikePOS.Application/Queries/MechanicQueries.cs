using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Queries;

public record MechanicWorkloadDto(
    List<Mechanic> Mechanics,
    Dictionary<string, List<MechanicTicketDto>> TicketsByMechanic,
    int TotalOpen,
    int TotalInProgress,
    int TotalWaiting,
    int Unassigned);

public record MechanicTicketDto(
    string Id, string TicketDisplay, string? ComponentName, string? ComponentType,
    TicketStatus Status, DateTime CreatedAt);

public class GetMechanicByIdQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public GetMechanicByIdQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<Mechanic?> HandleAsync(string id, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Mechanic.FindAsync(new object[] { id }, ct);
    }
}

public class ListMechanicsQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public ListMechanicsQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<Mechanic>> HandleAsync(CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Mechanic.OrderBy(m => m.Name).ToListAsync(ct);
    }
}

public class GetMechanicWorkloadQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public GetMechanicWorkloadQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<MechanicWorkloadDto> HandleAsync(CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();

        var mechanics = await db.Mechanic.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync(ct);

        var activeStatuses = new[] { TicketStatus.Open, TicketStatus.InProgress, TicketStatus.WaitingForParts };
        var tickets = await db.ServiceTicket
            .Where(t => activeStatuses.Contains(t.Status))
            .Include(t => t.Component)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        var ticketsByMechanic = tickets
            .Where(t => t.MechanicId != null)
            .GroupBy(t => t.MechanicId!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => new MechanicTicketDto(
                    t.Id, t.TicketDisplay, t.Component?.Name, t.Component?.ComponentType,
                    t.Status, t.CreatedAt)).ToList());

        return new MechanicWorkloadDto(
            mechanics,
            ticketsByMechanic,
            tickets.Count(t => t.Status == TicketStatus.Open),
            tickets.Count(t => t.Status == TicketStatus.InProgress),
            tickets.Count(t => t.Status == TicketStatus.WaitingForParts),
            tickets.Count(t => t.MechanicId == null));
    }
}
