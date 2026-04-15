using System.Security.Claims;
using BikePOS.Api.Auth;
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

        g.MapGet("", async (HttpContext ctx, TenantContext tenant, MembershipResolver resolver, IDbContextFactory<BikePosContext> f, CancellationToken c) =>
        {
            var appUserId = ctx.User.FindFirstValue("app_user_id");
            if (string.IsNullOrEmpty(appUserId)) return Results.Unauthorized();

            var memberships = await resolver.ResolveAsync(appUserId, c);

            using var db = f.CreateDbContext();
            db.CurrentStoreId = null;
            var storeMeta = await db.Store.Include(s => s.Company).ThenInclude(co => co.Conglomerate)
                .Where(s => memberships.Select(m => m.StoreId).Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, c);

            var all = memberships.Select(m =>
            {
                var s = storeMeta[m.StoreId];
                return new SessionStoreDto(
                    s.Id, s.Name, s.IsActive,
                    s.CompanyId, s.Company.Name, s.Company.CountryCode,
                    s.Company.ConglomerateId, s.Company.Conglomerate.Name);
            }).ToList();

            var current = tenant.StoreId is null ? null : all.FirstOrDefault(x => x.Id == tenant.StoreId);
            return Results.Ok(new SessionDto(current, all));
        });
    }
}
