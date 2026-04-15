using BikePOS.Models;

namespace BikePOS.Services;

/// <summary>
/// Role hierarchy: higher rank implies every lower-rank permission.
/// Developer is the escape hatch (everything SuperAdmin + system-level).
/// </summary>
public static class Roles
{
    public static int Rank(StoreRole r) => r switch
    {
        StoreRole.Cashier => 10,
        StoreRole.Mechanic => 20,
        StoreRole.Admin => 30,
        StoreRole.SuperAdmin => 40,
        StoreRole.Developer => 50,
        _ => 0
    };

    public static bool Covers(StoreRole? actual, StoreRole required) =>
        actual.HasValue && Rank(actual.Value) >= Rank(required);
}

/// <summary>
/// Transport-agnostic authorization. Handlers (REST, MCP, background jobs, webhooks)
/// call <see cref="Require"/> with a permission token; the token strings are the same
/// ones the SPA consumes via <c>/api/auth/me</c>, so one vocabulary drives UI gating
/// and server-side enforcement.
/// </summary>
public class PermissionGuard
{
    private readonly TenantContext _tenant;
    public PermissionGuard(TenantContext tenant) => _tenant = tenant;

    public bool Has(string permission)
    {
        if (_tenant.Role is null) return false;
        return PermissionCatalog.For(_tenant.Role.Value).Contains(permission);
    }

    public void Require(string permission)
    {
        if (!Has(permission)) throw new ForbiddenException(permission);
    }
}

public class ForbiddenException : Exception
{
    public string Permission { get; }
    public ForbiddenException(string permission)
        : base($"Missing permission '{permission}' at the active store.")
        => Permission = permission;
}

/// <summary>
/// Canonical mapping of role → permission tokens. Kept next to the guard so
/// adding a token requires touching one file.
/// </summary>
public static class PermissionCatalog
{
    public static string[] For(StoreRole role)
    {
        var list = new List<string>();
        if (Roles.Covers(role, StoreRole.Cashier))
            list.AddRange(new[] { "pos.use", "tickets.view", "customers.view", "products.view", "services.view", "mechanics.view", "reports.view.own" });
        if (Roles.Covers(role, StoreRole.Mechanic))
            list.AddRange(new[] { "tickets.update.status", "tickets.update.own" });
        if (Roles.Covers(role, StoreRole.Admin))
            list.AddRange(new[] { "products.manage", "services.manage", "mechanics.manage", "customers.manage", "tickets.manage", "reports.view.all" });
        if (Roles.Covers(role, StoreRole.SuperAdmin))
            list.AddRange(new[] { "settings.manage", "users.manage", "stores.switch.any" });
        if (Roles.Covers(role, StoreRole.Developer))
            list.Add("system.admin");
        return list.Distinct().ToArray();
    }
}
