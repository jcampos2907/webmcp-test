using BikePOS.Models;
using BikePOS.Services;
using Microsoft.AspNetCore.Authorization;

namespace BikePOS.Api.Auth;

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

public class MinRoleRequirement : IAuthorizationRequirement
{
    public StoreRole Min { get; }
    public MinRoleRequirement(StoreRole min) => Min = min;
}

public class MinRoleHandler : AuthorizationHandler<MinRoleRequirement>
{
    private readonly TenantContext _tenant;
    public MinRoleHandler(TenantContext tenant) => _tenant = tenant;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext ctx, MinRoleRequirement req)
    {
        if (Roles.Covers(_tenant.Role, req.Min))
            ctx.Succeed(req);
        return Task.CompletedTask;
    }
}

public static class Policies
{
    public const string Cashier = "role.cashier";
    public const string Mechanic = "role.mechanic";
    public const string Admin = "role.admin";
    public const string SuperAdmin = "role.superadmin";
    public const string Developer = "role.developer";

    public static void Register(AuthorizationOptions o)
    {
        o.AddPolicy(Cashier, p => p.Requirements.Add(new MinRoleRequirement(StoreRole.Cashier)));
        o.AddPolicy(Mechanic, p => p.Requirements.Add(new MinRoleRequirement(StoreRole.Mechanic)));
        o.AddPolicy(Admin, p => p.Requirements.Add(new MinRoleRequirement(StoreRole.Admin)));
        o.AddPolicy(SuperAdmin, p => p.Requirements.Add(new MinRoleRequirement(StoreRole.SuperAdmin)));
        o.AddPolicy(Developer, p => p.Requirements.Add(new MinRoleRequirement(StoreRole.Developer)));
    }
}
