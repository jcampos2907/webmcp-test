using BikePOS.Data;
using BikePOS.Models;
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Auth;

public record EffectiveMembership(
    string StoreId,
    string StoreName,
    string CompanyId,
    string CompanyName,
    string ConglomerateId,
    string ConglomerateName,
    StoreRole Role,
    RoleScope ViaScope);

/// <summary>
/// Expands a user's (Store|Company|Conglomerate)-scoped role assignments
/// into the concrete list of stores they can access, picking the highest
/// role when multiple assignments cover the same store.
/// </summary>
public class MembershipResolver
{
    private readonly IDbContextFactory<BikePosContext> _factory;
    public MembershipResolver(IDbContextFactory<BikePosContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<EffectiveMembership>> ResolveAsync(string appUserId, CancellationToken ct = default)
    {
        using var db = _factory.CreateDbContext();
        db.CurrentStoreId = null;

        var assignments = await db.StoreUser
            .Where(su => su.AppUserId == appUserId)
            .ToListAsync(ct);
        if (assignments.Count == 0) return Array.Empty<EffectiveMembership>();

        var stores = await db.Store.Include(s => s.Company).ThenInclude(c => c.Conglomerate).ToListAsync(ct);

        var result = new Dictionary<string, EffectiveMembership>();
        foreach (var store in stores)
        {
            StoreRole? best = null;
            RoleScope via = RoleScope.Store;
            foreach (var a in assignments)
            {
                bool covers = a.Scope switch
                {
                    RoleScope.Store => a.StoreId == store.Id,
                    RoleScope.Company => a.CompanyId == store.CompanyId,
                    RoleScope.Conglomerate => a.ConglomerateId == store.Company.ConglomerateId,
                    _ => false
                };
                if (!covers) continue;
                if (best == null || Roles.Rank(a.Role) > Roles.Rank(best.Value))
                {
                    best = a.Role;
                    via = a.Scope;
                }
            }
            if (best.HasValue)
            {
                result[store.Id] = new EffectiveMembership(
                    store.Id, store.Name,
                    store.CompanyId, store.Company.Name,
                    store.Company.ConglomerateId, store.Company.Conglomerate.Name,
                    best.Value, via);
            }
        }
        return result.Values.OrderBy(m => m.ConglomerateName).ThenBy(m => m.CompanyName).ThenBy(m => m.StoreName).ToList();
    }

    public async Task<EffectiveMembership?> ResolveForStoreAsync(string appUserId, string storeId, CancellationToken ct = default)
    {
        var all = await ResolveAsync(appUserId, ct);
        return all.FirstOrDefault(m => m.StoreId == storeId);
    }
}
