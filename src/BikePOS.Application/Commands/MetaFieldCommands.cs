using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Commands;

/// <summary>Request to save entity meta values (upsert pattern).</summary>
public record SaveEntityMetaValuesRequest(
    string EntityType,
    string EntityId,
    List<MetaFieldDefinition> Fields,
    Dictionary<string, string> Values,
    Func<MetaFieldDefinition, bool> IsFieldVisible);

/// <summary>
/// Saves EntityMetaValue records for any entity type using upsert logic:
/// updates existing records, creates new ones, nulls hidden fields.
/// </summary>
public class SaveEntityMetaValuesCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public SaveEntityMetaValuesCommandHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task HandleAsync(SaveEntityMetaValuesRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var existing = await db.EntityMetaValue
            .Where(mv => mv.EntityType == request.EntityType && mv.EntityId == request.EntityId)
            .ToListAsync(ct);

        foreach (var field in request.Fields)
        {
            var isVisible = request.IsFieldVisible(field);
            var val = request.Values.TryGetValue(field.Id, out var v) ? v : null;
            var record = existing.FirstOrDefault(mv => mv.MetaFieldDefinitionId == field.Id);

            if (!isVisible) { if (record is not null) record.Value = null; continue; }

            if (record is not null)
                record.Value = val;
            else if (!string.IsNullOrWhiteSpace(val))
                db.EntityMetaValue.Add(new EntityMetaValue
                {
                    EntityType = request.EntityType,
                    EntityId = request.EntityId,
                    MetaFieldDefinitionId = field.Id,
                    Value = val
                });
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Simplified overload for creating new entity meta values (no existing records to upsert).
    /// Used when saving meta values for a newly created entity.
    /// </summary>
    public async Task CreateAsync(string entityType, string entityId,
        List<MetaFieldDefinition> fields, Dictionary<string, string> values,
        Func<MetaFieldDefinition, bool> isFieldVisible, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        foreach (var field in fields)
        {
            var val = values.TryGetValue(field.Id, out var v) ? v : null;
            if (!string.IsNullOrWhiteSpace(val) && isFieldVisible(field))
            {
                db.EntityMetaValue.Add(new EntityMetaValue
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    MetaFieldDefinitionId = field.Id,
                    Value = val
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Request to save customer meta values (upsert pattern).</summary>
public record SaveCustomerMetaValuesRequest(
    string CustomerId,
    List<MetaFieldDefinition> Fields,
    Dictionary<string, string> Values,
    Func<MetaFieldDefinition, bool> IsFieldVisible);

/// <summary>
/// Saves CustomerMetaValue records using upsert logic.
/// </summary>
public class SaveCustomerMetaValuesCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public SaveCustomerMetaValuesCommandHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task HandleAsync(SaveCustomerMetaValuesRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var existing = await db.CustomerMetaValue
            .Where(mv => mv.CustomerId == request.CustomerId)
            .ToListAsync(ct);

        foreach (var field in request.Fields)
        {
            var isVisible = request.IsFieldVisible(field);
            var val = request.Values.TryGetValue(field.Id, out var v) ? v : null;
            var record = existing.FirstOrDefault(mv => mv.MetaFieldDefinitionId == field.Id);

            if (!isVisible) { if (record is not null) record.Value = null; continue; }

            if (record is not null)
                record.Value = val;
            else if (!string.IsNullOrWhiteSpace(val))
                db.CustomerMetaValue.Add(new CustomerMetaValue
                {
                    CustomerId = request.CustomerId,
                    MetaFieldDefinitionId = field.Id,
                    Value = val
                });
        }

        await db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Applies country-specific meta field presets for a given entity type.
/// Inserts missing presets and fixes conditional field references.
/// </summary>
public class ApplyCountryPresetsCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public ApplyCountryPresetsCommandHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task HandleAsync(string companyId, string entityType, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var company = await db.Company.FindAsync(new object[] { companyId }, ct);
        if (company is null || string.IsNullOrEmpty(company.CountryCode)) return;

        var presets = MetaFieldDefinition.GetPresetsForCountry(company.CountryCode, entityType);
        if (!presets.Any()) return;

        var existing = await db.MetaFieldDefinition
            .Where(f => f.EntityType == entityType && f.CompanyId == companyId)
            .ToListAsync(ct);
        var existingByKey = existing.ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);
        var presetKeyToDbId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var changed = false;

        foreach (var preset in presets)
        {
            if (existingByKey.TryGetValue(preset.Key, out var dbField))
                presetKeyToDbId[preset.Key] = dbField.Id;
            else
            {
                preset.CompanyId = companyId;
                db.MetaFieldDefinition.Add(preset);
                presetKeyToDbId[preset.Key] = preset.Id;
                changed = true;
            }
        }

        foreach (var preset in presets)
        {
            if (preset.ConditionalOnFieldId == null) continue;
            if (!existingByKey.TryGetValue(preset.Key, out var dbField)) continue;
            var parentPreset = presets.FirstOrDefault(p => p.Id == preset.ConditionalOnFieldId);
            if (parentPreset == null) continue;
            var parentDbId = presetKeyToDbId.GetValueOrDefault(parentPreset.Key);
            if (parentDbId != null && (dbField.ConditionalOnFieldId != parentDbId || dbField.ConditionalOnValue != preset.ConditionalOnValue))
            {
                dbField.ConditionalOnFieldId = parentDbId;
                dbField.ConditionalOnValue = preset.ConditionalOnValue;
                changed = true;
            }
        }

        if (changed) await db.SaveChangesAsync(ct);
    }
}
