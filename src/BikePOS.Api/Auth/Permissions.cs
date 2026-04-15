// Policy + MinRoleHandler are retained only for potential future endpoint-level
// gating. Current auth model: PermissionGuard (in BikePOS.Services) is called
// inside every mutating handler — REST and MCP share the same vocabulary.
using BikePOS.Models;
using BikePOS.Services;
using Microsoft.AspNetCore.Authorization;

namespace BikePOS.Api.Auth;

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
