using BikePOS.Application.Queries;

namespace BikePOS.Api.Endpoints;

public static class DashboardEndpoints
{
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
    }
}
