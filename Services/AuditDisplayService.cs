using Microsoft.EntityFrameworkCore;
using BikePOS.Data;

namespace BikePOS.Services;

/// <summary>
/// Resolves audit field identifiers (e.g. "sub:abc123", "uid:xyz") to human-readable display names
/// by looking up the AppUser table. Caches results per request scope.
/// </summary>
public class AuditDisplayService
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly Dictionary<string, string> _cache = new();

    public AuditDisplayService(IDbContextFactory<BikePosContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string> ResolveAsync(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return "—";

        if (_cache.TryGetValue(identifier, out var cached)) return cached;

        string? displayName = null;

        using var context = _dbFactory.CreateDbContext();

        if (identifier.StartsWith("sub:"))
        {
            var sub = identifier[4..];
            displayName = await context.AppUser
                .Where(u => u.ExternalSubjectId == sub)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync();
        }
        else if (identifier.StartsWith("uid:"))
        {
            var uid = identifier[4..];
            displayName = await context.AppUser
                .Where(u => u.Id == uid)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync();
        }

        var result = displayName ?? identifier;
        _cache[identifier] = result;
        return result;
    }
}
