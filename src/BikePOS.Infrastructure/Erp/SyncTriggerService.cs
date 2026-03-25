using Microsoft.Extensions.Logging;

namespace BikePOS.Infrastructure.Erp;

/// <summary>
/// Lightweight trigger that fires outbound ERP sync after entity CRUD operations.
/// Uses fire-and-forget pattern to avoid blocking the UI.
/// </summary>
public class SyncTriggerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncTriggerService> _logger;

    public SyncTriggerService(IServiceScopeFactory scopeFactory, ILogger<SyncTriggerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Fire-and-forget push of an entity to all active ERP connections.</summary>
    public void TriggerPush(string entityType, string entityId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ErpSyncService>();
                await syncService.PushEntityAsync(entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background ERP sync failed for {EntityType} {EntityId}", entityType, entityId);
            }
        });
    }
}
