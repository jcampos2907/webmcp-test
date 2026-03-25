using System.Reflection;
using System.Text.Json;
using BikePOS.Data;
using BikePOS.Interfaces.Services;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Infrastructure.Erp;

public class ErpSyncService
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly IEnumerable<IErpAdapter> _adapters;
    private readonly ILogger<ErpSyncService> _logger;

    public ErpSyncService(
        IDbContextFactory<BikePosContext> dbFactory,
        IEnumerable<IErpAdapter> adapters,
        ILogger<ErpSyncService> logger)
    {
        _dbFactory = dbFactory;
        _adapters = adapters;
        _logger = logger;
    }

    /// <summary>Push a local entity to all active ERP connections that sync this entity type.</summary>
    public async Task PushEntityAsync(string entityType, string entityId)
    {
        using var db = _dbFactory.CreateDbContext();

        var connections = await db.ErpConnection
            .Include(c => c.FieldMappings)
            .Where(c => c.IsActive)
            .ToListAsync();

        foreach (var conn in connections)
        {
            if (!IsEntitySyncEnabled(conn, entityType))
                continue;

            var adapter = _adapters.FirstOrDefault(a => a.ProviderName == conn.Provider);
            if (adapter == null)
            {
                _logger.LogWarning("No adapter found for provider {Provider}", conn.Provider);
                continue;
            }

            var payload = await BuildPayloadAsync(db, entityType, entityId, conn);
            if (payload == null) continue;

            var log = new SyncLog
            {
                ErpConnectionId = conn.Id,
                Direction = SyncDirection.Outbound,
                EntityType = entityType,
                EntityId = entityId,
                StoreId = conn.StoreId,
                RequestPayload = JsonSerializer.Serialize(payload.Fields)
            };
            db.SyncLog.Add(log);
            await db.SaveChangesAsync();

            var result = await adapter.PushEntityAsync(conn, payload);

            log.Status = result.Success ? SyncStatus.Success : SyncStatus.Failed;
            log.ResponsePayload = result.ResponsePayload;
            log.ErrorMessage = result.ErrorMessage;
            log.CompletedAt = DateTime.UtcNow;

            // Assign external ID back to entity if this was a create
            if (result.Success && !string.IsNullOrEmpty(result.ExternalId) && string.IsNullOrEmpty(payload.ExternalId))
            {
                await SetExternalIdAsync(db, entityType, entityId, result.ExternalId, conn.Provider);
            }

            await db.SaveChangesAsync();
        }
    }

    /// <summary>Pull an entity from ERP by its external ID and return the raw fields.</summary>
    public async Task<ErpSyncResult?> PullEntityAsync(string connectionId, string entityType, string externalId)
    {
        using var db = _dbFactory.CreateDbContext();
        var conn = await db.ErpConnection.FindAsync(connectionId);
        if (conn == null) return null;

        var adapter = _adapters.FirstOrDefault(a => a.ProviderName == conn.Provider);
        if (adapter == null) return null;

        var log = new SyncLog
        {
            ErpConnectionId = conn.Id,
            Direction = SyncDirection.Inbound,
            EntityType = entityType,
            StoreId = conn.StoreId,
            RequestPayload = $"{{\"external_id\":\"{externalId}\"}}"
        };
        db.SyncLog.Add(log);
        await db.SaveChangesAsync();

        var result = await adapter.PullEntityAsync(conn, entityType, externalId);

        log.Status = result.Success ? SyncStatus.Success : SyncStatus.Failed;
        log.ResponsePayload = result.ResponsePayload;
        log.ErrorMessage = result.ErrorMessage;
        log.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return result;
    }

    public async Task<bool> TestConnectionAsync(string connectionId)
    {
        using var db = _dbFactory.CreateDbContext();
        var conn = await db.ErpConnection.FindAsync(connectionId);
        if (conn == null) return false;

        var adapter = _adapters.FirstOrDefault(a => a.ProviderName == conn.Provider);
        return adapter != null && await adapter.TestConnectionAsync(conn);
    }

    public async Task<List<SyncLog>> GetRecentLogsAsync(int count = 50)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.SyncLog
            .Include(s => s.ErpConnection)
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    private async Task<ErpEntityPayload?> BuildPayloadAsync(BikePosContext db, string entityType, string entityId, ErpConnection conn)
    {
        var mappings = conn.FieldMappings
            .Where(m => m.EntityType == entityType)
            .OrderBy(m => m.SortOrder)
            .ToList();

        object? entity = entityType switch
        {
            "Customer" => await db.Customer.FindAsync(entityId),
            "Component" => await db.Component.FindAsync(entityId),
            "Product" => await db.Product.FindAsync(entityId),
            "ServiceTicket" => await db.ServiceTicket.FindAsync(entityId),
            "Charge" => await db.Charge.FindAsync(entityId),
            _ => null
        };

        if (entity == null) return null;

        // Use anti-corruption layer translators when no custom mappings
        Dictionary<string, object?> fields;
        if (mappings.Count > 0)
        {
            fields = new Dictionary<string, object?>();
            var entityProps = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var mapping in mappings)
            {
                var prop = entityProps.FirstOrDefault(p => p.Name == mapping.LocalField);
                if (prop == null) continue;
                var value = prop.GetValue(entity);
                value = ApplyTransform(value, mapping.TransformExpression);
                fields[mapping.RemoteField] = value;
            }
        }
        else
        {
            fields = entityType switch
            {
                "Customer" => ErpEntityTranslator.TranslateCustomer((Customer)entity),
                "Product" => ErpEntityTranslator.TranslateProduct((Product)entity),
                "Component" => ErpEntityTranslator.TranslateComponent((Component)entity),
                "ServiceTicket" => ErpEntityTranslator.TranslateServiceTicket((ServiceTicket)entity),
                "Charge" => ErpEntityTranslator.TranslateCharge((Charge)entity),
                _ => BuildDefaultFields(entity)
            };
        }

        var externalIdProp = entity.GetType().GetProperty("ExternalId");
        var externalId = externalIdProp?.GetValue(entity) as string;

        return new ErpEntityPayload
        {
            EntityType = entityType,
            EntityId = entityId,
            ExternalId = externalId,
            Fields = fields
        };
    }

    private static Dictionary<string, object?> BuildDefaultFields(object entity)
    {
        var fields = new Dictionary<string, object?>();
        foreach (var prop in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string)) continue;
            if (prop.Name == "Store") continue;
            fields[prop.Name] = prop.GetValue(entity);
        }
        return fields;
    }

    private static object? ApplyTransform(object? value, string? transform)
    {
        if (string.IsNullOrEmpty(transform) || value == null) return value;

        return transform.ToLower() switch
        {
            "toupper" => value.ToString()?.ToUpperInvariant(),
            "tolower" => value.ToString()?.ToLowerInvariant(),
            "tostring" => value.ToString(),
            _ => value
        };
    }

    private static async Task SetExternalIdAsync(BikePosContext db, string entityType, string entityId, string externalId, string source)
    {
        switch (entityType)
        {
            case "Customer":
                var customer = await db.Customer.FindAsync(entityId);
                if (customer != null) { customer.ExternalId = externalId; customer.ExternalSource = source; }
                break;
            case "Component":
                var component = await db.Component.FindAsync(entityId);
                if (component != null) { component.ExternalId = externalId; component.ExternalSource = source; }
                break;
            case "Product":
                var product = await db.Product.FindAsync(entityId);
                if (product != null) { product.ExternalId = externalId; product.ExternalSource = source; }
                break;
            case "ServiceTicket":
                var ticket = await db.ServiceTicket.FindAsync(entityId);
                if (ticket != null) { ticket.ExternalId = externalId; ticket.ExternalSource = source; }
                break;
            case "Charge":
                var charge = await db.Charge.FindAsync(entityId);
                if (charge != null) { charge.ExternalId = externalId; charge.ExternalSource = source; }
                break;
        }
    }

    private static bool IsEntitySyncEnabled(ErpConnection conn, string entityType)
    {
        return entityType switch
        {
            "Customer" => conn.SyncCustomers,
            "Component" => conn.SyncComponents,
            "Product" => conn.SyncProducts,
            "ServiceTicket" => conn.SyncTickets,
            "Charge" => conn.SyncCharges,
            _ => false
        };
    }
}
