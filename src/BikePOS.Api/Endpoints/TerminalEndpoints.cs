using BikePOS.Application.Commands;
using BikePOS.Data;
using BikePOS.Interfaces.Services;
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Endpoints;

public static class TerminalEndpoints
{
    public record TerminalListItemDto(string Id, string Name, string Provider, bool IsActive);

    public static void MapTerminalPublicEndpoints(this WebApplication app)
    {
        app.MapGet("/api/terminals", async (
            TenantContext tenant, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var q = db.PaymentTerminal.Where(t => t.IsActive);
            if (!string.IsNullOrEmpty(tenant.StoreId))
                q = q.Where(t => t.StoreId == tenant.StoreId);
            var items = await q.OrderBy(t => t.Name).ToListAsync(ct);
            return Results.Ok(items.Select(t => new TerminalListItemDto(
                t.Id, t.Name, t.Provider.ToString(), t.IsActive)));
        });

        var s = app.MapGroup("/api/payment-sessions");

        s.MapGet("/{id}/status", async (string id, ProcessChargeCommandHandler h, CancellationToken ct) =>
        {
            var status = await h.PollTerminalStatusAsync(id, ct);
            return Results.Ok(new { status = status.ToString() });
        });

        s.MapPost("/{id}/confirm", async (string id, ProcessChargeCommandHandler h, CancellationToken ct) =>
        {
            var result = await h.ConfirmTerminalPaymentAsync(id, ct);
            return result.ErrorMessage is null ? Results.Ok(result) : Results.BadRequest(result);
        });

        s.MapPost("/{id}/cancel", async (string id, ProcessChargeCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.CancelTerminalPaymentAsync(id, ct);
            return Results.Ok(new { cancelled = ok });
        });
    }
}
