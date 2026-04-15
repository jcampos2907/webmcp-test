using System.ComponentModel;
using BikePOS.Data;
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace BikePOS.Api.Mcp;

/// <summary>
/// Shop-facing tools exposed over MCP. Every tool runs inside the caller's request
/// scope so it inherits the auth cookie + TenantContext — store scoping is enforced
/// by the same middleware chain as the REST endpoints.
/// </summary>
[McpServerToolType]
public class BikePosTools
{
    public record ServiceDto(string Id, string Name, string? Description, decimal DefaultPrice, int? EstimatedMinutes);

    [McpServerTool(Name = "list_services")]
    [Description("Lists all services available at the caller's currently-active store. Each service has an id, name, optional description, default price, and estimated duration.")]
    public static async Task<IReadOnlyList<ServiceDto>> ListServices(
        IDbContextFactory<BikePosContext> factory,
        TenantContext tenant,
        CancellationToken ct)
    {
        using var db = factory.CreateDbContext();
        db.CurrentStoreId = tenant.StoreId;
        var items = await db.Service.OrderBy(s => s.Name).ToListAsync(ct);
        return items.Select(s => new ServiceDto(s.Id, s.Name, s.Description, s.DefaultPrice, s.EstimatedMinutes)).ToList();
    }
}
