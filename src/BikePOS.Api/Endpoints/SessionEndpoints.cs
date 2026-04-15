using BikePOS.Data;
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Endpoints;

public static class SessionEndpoints
{
    public record SessionStoreDto(string Id, string Name, bool IsActive, string CompanyId, string CompanyName, string? CountryCode, string ConglomerateId, string ConglomerateName);
    public record SessionDto(
        SessionStoreDto? CurrentStore,
        List<SessionStoreDto> AvailableStores);

    public static void MapSessionEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/session");

        g.MapGet("", async (TenantContext tenant, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            using var db = f.CreateDbContext();
            // Bypass store filter so the picker always sees every store
            db.CurrentStoreId = null;
            var stores = await db.Store
                .Include(s => s.Company).ThenInclude(c => c.Conglomerate)
                .OrderBy(s => s.Company.Name).ThenBy(s => s.Name)
                .ToListAsync(ct);
            var all = stores.Select(s => new SessionStoreDto(
                s.Id, s.Name, s.IsActive,
                s.CompanyId, s.Company.Name, s.Company.CountryCode,
                s.Company.ConglomerateId, s.Company.Conglomerate.Name)).ToList();
            var current = tenant.StoreId is null ? null : all.FirstOrDefault(x => x.Id == tenant.StoreId);
            return Results.Ok(new SessionDto(current, all));
        });
    }
}
