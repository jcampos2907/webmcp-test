using BikePOS.Models;

namespace BikePOS.Interfaces.Services;

public class ErpEntityPayload
{
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? ExternalId { get; set; }
    public Dictionary<string, object?> Fields { get; set; } = new();
}

public class ErpSyncResult
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IErpAdapter
{
    string ProviderName { get; }
    Task<ErpSyncResult> PushEntityAsync(ErpConnection connection, ErpEntityPayload payload);
    Task<ErpSyncResult> PullEntityAsync(ErpConnection connection, string entityType, string externalId);
    Task<bool> TestConnectionAsync(ErpConnection connection);
    Task<List<string>> GetRemoteFieldsAsync(ErpConnection connection, string entityType);
}
