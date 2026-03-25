using BikePOS.Data;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Queries;

public record DailySalesRow(
    DateOnly Date,
    decimal Revenue,
    int Transactions,
    decimal Cash,
    decimal Card,
    decimal Transfer);

public class DailySalesQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public DailySalesQueryHandler(IDbContextFactory<BikePosContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<DailySalesRow>> HandleAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var charges = await db.Charge
            .Where(c => c.PaymentStatus == Models.PaymentStatus.Completed
                        && c.ChargedAt >= fromDt && c.ChargedAt <= toDt)
            .Select(c => new
            {
                Date = c.ChargedAt.Date,
                c.Amount,
                c.PaymentMethod
            })
            .ToListAsync(ct);

        return charges
            .GroupBy(c => DateOnly.FromDateTime(c.Date))
            .Select(g => new DailySalesRow(
                g.Key,
                g.Sum(c => c.Amount),
                g.Count(),
                g.Where(c => c.PaymentMethod == Models.PaymentMethod.Cash).Sum(c => c.Amount),
                g.Where(c => c.PaymentMethod == Models.PaymentMethod.Card).Sum(c => c.Amount),
                g.Where(c => c.PaymentMethod == Models.PaymentMethod.Transfer).Sum(c => c.Amount)))
            .OrderBy(r => r.Date)
            .ToList();
    }
}
