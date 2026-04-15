using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Endpoints;

public static class ComponentEndpoints
{
    public record CreateComponentDto(
        string Name,
        string ComponentType,
        string? CustomerId,
        string? Brand,
        string? Color,
        string? Sku,
        decimal Price,
        string? StoreId);

    public static void MapComponentEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/components");

        g.MapGet("", async (IDbContextFactory<BikePosContext> f, string? customerId, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var q = db.Component.AsQueryable();
            if (!string.IsNullOrEmpty(customerId))
                q = q.Where(c => c.CustomerId == customerId);
            var list = await q.OrderBy(c => c.Name).ToListAsync(ct);
            return Results.Ok(list);
        });

        g.MapGet("/{id}", async (string id, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var c = await db.Component.FirstOrDefaultAsync(x => x.Id == id, ct);
            return c is null ? Results.NotFound() : Results.Ok(c);
        });

        g.MapPost("", async (CreateComponentDto body, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            var c = new Component
            {
                Name = body.Name,
                ComponentType = body.ComponentType,
                CustomerId = body.CustomerId,
                Brand = body.Brand ?? "",
                Color = body.Color ?? "",
                Sku = body.Sku ?? "SKU" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                Price = body.Price,
                StoreId = body.StoreId,
            };
            db.Component.Add(c);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/components/{c.Id}", c);
        });
    }
}
