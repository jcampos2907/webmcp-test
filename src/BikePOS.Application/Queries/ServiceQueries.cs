using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Queries;

public record ServiceDto(string Id, string Name, string? Description, decimal DefaultPrice, int? EstimatedMinutes);

public class GetServiceByIdQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public GetServiceByIdQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<Service?> HandleAsync(string id, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Service.FindAsync(new object[] { id }, ct);
    }
}

public class ListServicesQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public ListServicesQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<Service>> HandleAsync(CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Service.OrderBy(s => s.Name).ToListAsync(ct);
    }
}
