using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Queries;

/// <summary>
/// Loads active MetaFieldDefinition records for a given entity type and company.
/// </summary>
public class ListMetaFieldsQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public ListMetaFieldsQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<MetaFieldDefinition>> HandleAsync(string entityType, string? companyId, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.MetaFieldDefinition
            .Where(f => f.IsActive && f.EntityType == entityType && f.CompanyId == companyId)
            .OrderBy(f => f.SortOrder).ThenBy(f => f.Label)
            .ToListAsync(ct);
    }
}

/// <summary>
/// Loads BaseFieldLayout records for a given entity type and company.
/// Auto-seeds defaults if none exist.
/// </summary>
public class LoadBaseFieldLayoutsQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public LoadBaseFieldLayoutsQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<BaseFieldLayout>> HandleAsync(string entityType, string? companyId, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var layouts = await db.BaseFieldLayout
            .Where(b => b.EntityType == entityType && b.CompanyId == companyId)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(ct);

        if (!layouts.Any())
        {
            var defaults = BaseFieldLayout.GetBaseFields(entityType);
            foreach (var d in defaults) d.CompanyId = companyId;
            db.BaseFieldLayout.AddRange(defaults);
            await db.SaveChangesAsync(ct);
            layouts = defaults;
        }

        return layouts;
    }
}

/// <summary>
/// Loads EntityMetaValue records for a given entity type and entity ID.
/// Returns a dictionary of MetaFieldDefinitionId → Value.
/// </summary>
public class LoadEntityMetaValuesQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public LoadEntityMetaValuesQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<Dictionary<string, string>> HandleAsync(string entityType, string entityId, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var values = await db.EntityMetaValue
            .Where(mv => mv.EntityType == entityType && mv.EntityId == entityId)
            .ToListAsync(ct);

        return values.ToDictionary(mv => mv.MetaFieldDefinitionId, mv => mv.Value ?? "");
    }
}

/// <summary>
/// Loads CustomerMetaValue records for a given customer ID.
/// Returns a dictionary of MetaFieldDefinitionId → Value.
/// </summary>
public class LoadCustomerMetaValuesQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public LoadCustomerMetaValuesQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<Dictionary<string, string>> HandleAsync(string customerId, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var values = await db.CustomerMetaValue
            .Where(mv => mv.CustomerId == customerId)
            .ToListAsync(ct);

        return values.ToDictionary(mv => mv.MetaFieldDefinitionId, mv => mv.Value ?? "");
    }
}
