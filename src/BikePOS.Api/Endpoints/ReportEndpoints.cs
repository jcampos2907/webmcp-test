using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Endpoints;

public static class ReportEndpoints
{
    public record DailySalesReportRow(DateOnly Date, decimal Revenue, int Transactions, decimal Cash, decimal Card, decimal Transfer);
    public record ServiceRevenueReportRow(string ServiceName, decimal Revenue, int TicketCount);
    public record MechanicProductivityReportRow(string MechanicName, int TicketCount, double AvgHoursToComplete);

    public static void MapReportEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/reports");

        g.MapGet("/daily-sales", async (IDbContextFactory<BikePosContext> f, DateOnly from, DateOnly to, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var fromDt = from.ToDateTime(TimeOnly.MinValue);
            var toDt = to.ToDateTime(TimeOnly.MaxValue);

            var charges = await db.Charge
                .Where(c => c.PaymentStatus == PaymentStatus.Completed && c.ChargedAt >= fromDt && c.ChargedAt <= toDt)
                .Select(c => new { Date = c.ChargedAt.Date, c.Amount, c.PaymentMethod })
                .ToListAsync(ct);

            var rows = charges
                .GroupBy(c => DateOnly.FromDateTime(c.Date))
                .Select(grp => new DailySalesReportRow(
                    grp.Key,
                    grp.Sum(c => c.Amount),
                    grp.Count(),
                    grp.Where(c => c.PaymentMethod == PaymentMethod.Cash).Sum(c => c.Amount),
                    grp.Where(c => c.PaymentMethod == PaymentMethod.Card).Sum(c => c.Amount),
                    grp.Where(c => c.PaymentMethod == PaymentMethod.Transfer).Sum(c => c.Amount)))
                .OrderBy(r => r.Date)
                .ToList();
            return Results.Ok(rows);
        });

        g.MapGet("/service-revenue", async (IDbContextFactory<BikePosContext> f, DateOnly from, DateOnly to, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var fromDt = from.ToDateTime(TimeOnly.MinValue);
            var toDt = to.ToDateTime(TimeOnly.MaxValue);

            var rows = await db.Charge
                .Where(c => c.PaymentStatus == PaymentStatus.Completed && c.ChargedAt >= fromDt && c.ChargedAt <= toDt)
                .Include(c => c.ServiceTicket).ThenInclude(t => t.BaseService)
                .Select(c => new
                {
                    ServiceName = c.ServiceTicket.BaseService != null ? c.ServiceTicket.BaseService.Name : "No Service",
                    c.Amount,
                    c.ServiceTicketId
                })
                .ToListAsync(ct);

            var result = rows
                .GroupBy(r => r.ServiceName)
                .Select(grp => new ServiceRevenueReportRow(
                    grp.Key,
                    grp.Sum(r => r.Amount),
                    grp.Select(r => r.ServiceTicketId).Distinct().Count()))
                .OrderByDescending(r => r.Revenue)
                .ToList();
            return Results.Ok(result);
        });

        g.MapGet("/mechanic-productivity", async (IDbContextFactory<BikePosContext> f, DateOnly from, DateOnly to, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var fromDt = from.ToDateTime(TimeOnly.MinValue);
            var toDt = to.ToDateTime(TimeOnly.MaxValue);

            var tickets = await db.ServiceTicket
                .Where(t => t.Mechanic != null && t.Status == TicketStatus.Charged && t.UpdatedAt >= fromDt && t.UpdatedAt <= toDt)
                .Select(t => new { MechanicName = t.Mechanic!.Name, HoursToComplete = (t.UpdatedAt - t.CreatedAt).TotalHours })
                .ToListAsync(ct);

            var result = tickets
                .GroupBy(t => t.MechanicName)
                .Select(grp => new MechanicProductivityReportRow(grp.Key, grp.Count(), Math.Round(grp.Average(t => t.HoursToComplete), 1)))
                .OrderByDescending(r => r.TicketCount)
                .ToList();
            return Results.Ok(result);
        });
    }
}
