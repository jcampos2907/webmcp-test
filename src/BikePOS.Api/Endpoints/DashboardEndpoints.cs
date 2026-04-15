using BikePOS.Application.Queries;
using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Endpoints;

public static class DashboardEndpoints
{
    public record KpisDto(decimal TodayRevenue, int TodayTransactions, int OpenTickets, int ReadyToCharge);
    public record RecentChargeDto(string Id, decimal Amount, string PaymentMethod, string? TicketDisplay, string? TicketId, DateTime ChargedAt);

    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/dashboard");

        g.MapGet("/daily-sales", async (DailySalesQueryHandler h, DateOnly? from, DateOnly? to, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var toDate = to ?? today;
            var fromDate = from ?? toDate.AddDays(-6);
            return Results.Ok(await h.HandleAsync(fromDate, toDate, ct));
        });

        g.MapGet("/kpis", async (IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);

            var todayCharges = await db.Charge
                .Where(c => c.PaymentStatus == PaymentStatus.Completed && c.ChargedAt >= todayStart && c.ChargedAt < todayEnd)
                .Select(c => c.Amount)
                .ToListAsync(ct);

            var openCount = await db.ServiceTicket.CountAsync(t =>
                t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.WaitingForParts, ct);
            var readyCount = await db.ServiceTicket.CountAsync(t => t.Status == TicketStatus.Completed, ct);

            return Results.Ok(new KpisDto(todayCharges.Sum(), todayCharges.Count, openCount, readyCount));
        });

        g.MapGet("/recent-charges", async (IDbContextFactory<BikePosContext> f, int? take, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var rows = await db.Charge
                .Include(c => c.ServiceTicket)
                .OrderByDescending(c => c.ChargedAt)
                .Take(take ?? 10)
                .Select(c => new RecentChargeDto(
                    c.Id, c.Amount, c.PaymentMethod.ToString(),
                    c.ServiceTicket != null ? c.ServiceTicket.TicketDisplay : null,
                    c.ServiceTicketId,
                    c.ChargedAt))
                .ToListAsync(ct);
            return Results.Ok(rows);
        });
    }
}
