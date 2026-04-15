using System.Security.Claims;
using BikePOS.Data;
using BikePOS.Models;
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Endpoints;

public static class AdminEndpoints
{
    // --- Organization DTOs ---
    public record ConglomerateDto(string Id, string Name, List<CompanyDto> Companies);
    public record CompanyDto(string Id, string ConglomerateId, string Name, string Locale, string Currency, string? TaxId, string? CountryCode, List<StoreDto> Stores);
    public record StoreDto(string Id, string CompanyId, string Name, string? Address, string? Phone, string? Email, bool IsActive);

    public record UpsertConglomerateDto(string Name);
    public record UpsertCompanyDto(string ConglomerateId, string Name, string Locale, string Currency, string? TaxId, string? CountryCode);
    public record UpsertStoreDto(string CompanyId, string Name, string? Address, string? Phone, string? Email, bool IsActive);

    // --- Terminal DTOs ---
    public record TerminalDto(string Id, string StoreId, string StoreName, string Name, string IpAddress, int Port, string Provider, bool IsActive, DateTime? LastSeenAt);
    public record UpsertTerminalDto(string StoreId, string Name, string IpAddress, int Port, string Provider, bool IsActive);

    // --- User DTOs ---
    public record UserRoleDto(string StoreUserId, string Scope, string? StoreId, string? StoreName, string? CompanyId, string? CompanyName, string? ConglomerateId, string? ConglomerateName, string Role);
    public record UserDto(string Id, string? DisplayName, string? Email, string ExternalSubjectId, DateTime? LastLoginAt, DateTime CreatedAt, List<UserRoleDto> Assignments);
    public record UpsertUserRoleDto(string Scope, string? StoreId, string? CompanyId, string? ConglomerateId, string Role);

    // --- OAuth DTOs ---
    public record OidcConfigDto(string Id, string ConglomerateId, string Authority, string ClientId, bool HasClientSecret, string ResponseType, string Scopes, bool MapInboundClaims, bool SaveTokens, bool GetClaimsFromUserInfoEndpoint, string? ProviderName, bool IsActive, DateTime UpdatedAt);
    public record UpsertOidcConfigDto(string ConglomerateId, string Authority, string ClientId, string? ClientSecret, string ResponseType, string Scopes, bool MapInboundClaims, bool SaveTokens, bool GetClaimsFromUserInfoEndpoint, string? ProviderName, bool IsActive);

    public static void MapAdminEndpoints(this WebApplication app)
    {
        MapOrganizationEndpoints(app);
        MapTerminalEndpoints(app);
        MapUserEndpoints(app);
        MapOidcEndpoints(app);
    }

    // ============ Organization ============
    static void MapOrganizationEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/api/admin/organization");

        g.MapGet("", async (PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var data = await db.Conglomerate
                .Include(c => c.Companies).ThenInclude(co => co.Stores)
                .AsSplitQuery()
                .ToListAsync(ct);
            return Results.Ok(data.Select(c => new ConglomerateDto(
                c.Id, c.Name,
                c.Companies.OrderBy(co => co.Name).Select(co => new CompanyDto(
                    co.Id, co.ConglomerateId, co.Name, co.Locale, co.Currency, co.TaxId, co.CountryCode,
                    co.Stores.OrderBy(s => s.Name).Select(s => new StoreDto(
                        s.Id, s.CompanyId, s.Name, s.Address, s.Phone, s.Email, s.IsActive)).ToList()
                )).ToList()
            )));
        });

        g.MapPut("/conglomerates/{id}", async (string id, UpsertConglomerateDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var c = await db.Conglomerate.FindAsync([id], ct);
            if (c is null) return Results.NotFound();
            c.Name = body.Name;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        g.MapPost("/conglomerates", async (UpsertConglomerateDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var c = new Conglomerate { Name = body.Name };
            db.Conglomerate.Add(c);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/admin/organization/conglomerates/{c.Id}", new { c.Id });
        });

        g.MapPost("/companies", async (UpsertCompanyDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var c = new Company
            {
                ConglomerateId = body.ConglomerateId,
                Name = body.Name,
                Locale = body.Locale,
                Currency = body.Currency,
                TaxId = body.TaxId,
                CountryCode = body.CountryCode,
            };
            db.Company.Add(c);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/admin/organization/companies/{c.Id}", new { c.Id });
        });

        g.MapPut("/companies/{id}", async (string id, UpsertCompanyDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var c = await db.Company.FindAsync([id], ct);
            if (c is null) return Results.NotFound();
            c.Name = body.Name;
            c.Locale = body.Locale;
            c.Currency = body.Currency;
            c.TaxId = body.TaxId;
            c.CountryCode = body.CountryCode;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        g.MapDelete("/companies/{id}", async (string id, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var c = await db.Company.Include(co => co.Stores).FirstOrDefaultAsync(co => co.Id == id, ct);
            if (c is null) return Results.NotFound();
            if (c.Stores.Count > 0) return Results.BadRequest(new { error = "Company has stores; delete them first" });
            db.Company.Remove(c);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        g.MapPost("/stores", async (UpsertStoreDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var s = new Store
            {
                CompanyId = body.CompanyId,
                Name = body.Name,
                Address = body.Address,
                Phone = body.Phone,
                Email = body.Email,
                IsActive = body.IsActive,
            };
            db.Store.Add(s);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/admin/organization/stores/{s.Id}", new { s.Id });
        });

        g.MapPut("/stores/{id}", async (string id, UpsertStoreDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var s = await db.Store.FindAsync([id], ct);
            if (s is null) return Results.NotFound();
            s.Name = body.Name;
            s.Address = body.Address;
            s.Phone = body.Phone;
            s.Email = body.Email;
            s.IsActive = body.IsActive;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        g.MapDelete("/stores/{id}", async (string id, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var s = await db.Store.FindAsync([id], ct);
            if (s is null) return Results.NotFound();
            db.Store.Remove(s);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    // ============ Terminals ============
    static void MapTerminalEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/api/admin/terminals");

        g.MapGet("", async (PermissionGuard guard, IDbContextFactory<BikePosContext> f, string? storeId, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var q = db.PaymentTerminal.Include(t => t.Store).AsQueryable();
            if (!string.IsNullOrEmpty(storeId)) q = q.Where(t => t.StoreId == storeId);
            var items = await q.OrderBy(t => t.Name).ToListAsync(ct);
            return Results.Ok(items.Select(t => new TerminalDto(
                t.Id, t.StoreId, t.Store?.Name ?? "", t.Name, t.IpAddress, t.Port,
                t.Provider.ToString(), t.IsActive, t.LastSeenAt)));
        });

        g.MapPost("", async (UpsertTerminalDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            if (!Enum.TryParse<TerminalProvider>(body.Provider, true, out var provider))
                return Results.BadRequest(new { error = "Invalid provider" });
            using var db = f.CreateDbContext();
            var t = new PaymentTerminal
            {
                StoreId = body.StoreId, Name = body.Name, IpAddress = body.IpAddress,
                Port = body.Port, Provider = provider, IsActive = body.IsActive,
            };
            db.PaymentTerminal.Add(t);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/admin/terminals/{t.Id}", new { t.Id });
        });

        g.MapPut("/{id}", async (string id, UpsertTerminalDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            if (!Enum.TryParse<TerminalProvider>(body.Provider, true, out var provider))
                return Results.BadRequest(new { error = "Invalid provider" });
            using var db = f.CreateDbContext();
            var t = await db.PaymentTerminal.FindAsync([id], ct);
            if (t is null) return Results.NotFound();
            t.StoreId = body.StoreId;
            t.Name = body.Name;
            t.IpAddress = body.IpAddress;
            t.Port = body.Port;
            t.Provider = provider;
            t.IsActive = body.IsActive;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        g.MapDelete("/{id}", async (string id, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var t = await db.PaymentTerminal.FindAsync([id], ct);
            if (t is null) return Results.NotFound();
            db.PaymentTerminal.Remove(t);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    // ============ Users ============
    static void MapUserEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/api/admin/users");

        g.MapGet("", async (PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("users.manage");
            using var db = f.CreateDbContext();
            var users = await db.AppUser
                .Include(u => u.StoreUsers).ThenInclude(su => su.Store).ThenInclude(s => s!.Company)
                .Include(u => u.StoreUsers).ThenInclude(su => su.Company)
                .Include(u => u.StoreUsers).ThenInclude(su => su.Conglomerate)
                .AsSplitQuery()
                .OrderByDescending(u => u.LastLoginAt ?? u.CreatedAt)
                .ToListAsync(ct);
            return Results.Ok(users.Select(u => new UserDto(
                u.Id, u.DisplayName, u.Email, u.ExternalSubjectId, u.LastLoginAt, u.CreatedAt,
                u.StoreUsers.Select(su => new UserRoleDto(
                    su.Id, su.Scope.ToString(),
                    su.StoreId, su.Store?.Name,
                    su.CompanyId, su.Company?.Name,
                    su.ConglomerateId, su.Conglomerate?.Name,
                    su.Role.ToString())).ToList())));
        });

        g.MapPost("/{userId}/roles", async (string userId, UpsertUserRoleDto body,
            PermissionGuard guard, TenantContext tenant, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("users.manage");
            if (!Enum.TryParse<StoreRole>(body.Role, true, out var role))
                return Results.BadRequest(new { error = "Invalid role" });
            if (!Enum.TryParse<RoleScope>(body.Scope, true, out var scope))
                return Results.BadRequest(new { error = "Invalid scope" });

            // Enforce the two legal shapes:
            //   SuperAdmin @ Conglomerate  — matching the active conglomerate
            //   {Cashier,Mechanic,Admin} @ Store — matching the active store
            // Anything else is rejected.
            if (role == StoreRole.SuperAdmin)
            {
                if (scope != RoleScope.Conglomerate || string.IsNullOrEmpty(body.ConglomerateId))
                    return Results.BadRequest(new { error = "SuperAdmin must be Conglomerate-scoped" });
                if (body.ConglomerateId != tenant.ConglomerateId)
                    return Results.BadRequest(new { error = "Can only grant SuperAdmin in the active conglomerate" });
            }
            else if (role == StoreRole.Developer)
            {
                return Results.BadRequest(new { error = "Developer role cannot be assigned from the UI" });
            }
            else
            {
                if (scope != RoleScope.Store || string.IsNullOrEmpty(body.StoreId))
                    return Results.BadRequest(new { error = "Non-SuperAdmin roles must be Store-scoped" });
                if (body.StoreId != tenant.StoreId)
                    return Results.BadRequest(new { error = "Can only grant store roles at the active store" });
            }

            using var db = f.CreateDbContext();
            db.CurrentStoreId = null;
            var existing = await db.StoreUser.FirstOrDefaultAsync(x =>
                x.AppUserId == userId && x.Scope == scope &&
                x.StoreId == body.StoreId && x.CompanyId == body.CompanyId && x.ConglomerateId == body.ConglomerateId, ct);
            if (existing is not null)
            {
                existing.Role = role;
            }
            else
            {
                db.StoreUser.Add(new StoreUser
                {
                    AppUserId = userId, Scope = scope,
                    StoreId = body.StoreId, CompanyId = body.CompanyId, ConglomerateId = body.ConglomerateId,
                    Role = role,
                });
            }
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        g.MapDelete("/roles/{storeUserId}", async (string storeUserId, HttpContext ctx,
            PermissionGuard guard, TenantContext tenant, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("users.manage");
            using var db = f.CreateDbContext();
            db.CurrentStoreId = null;
            var su = await db.StoreUser.FindAsync([storeUserId], ct);
            if (su is null) return Results.NotFound();

            // Scope-of-removal must match the actor's active context
            if (su.Scope == RoleScope.Conglomerate)
            {
                if (su.ConglomerateId != tenant.ConglomerateId)
                    return Results.BadRequest(new { error = "Out-of-scope removal" });
                // Prevent removing the last SuperAdmin in a conglomerate — would lock everyone out
                if (su.Role == StoreRole.SuperAdmin)
                {
                    var remaining = await db.StoreUser.CountAsync(x =>
                        x.Scope == RoleScope.Conglomerate && x.ConglomerateId == su.ConglomerateId && x.Role == StoreRole.SuperAdmin, ct);
                    if (remaining <= 1) return Results.BadRequest(new { error = "Cannot remove the last SuperAdmin" });
                }
                // Self-demotion: a SuperAdmin removing their own SuperAdmin role is allowed
                // as long as the guard above passes.
                _ = ctx.User.FindFirstValue("app_user_id");
            }
            else if (su.Scope == RoleScope.Store)
            {
                if (su.StoreId != tenant.StoreId)
                    return Results.BadRequest(new { error = "Can only revoke roles at the active store" });
            }
            else
            {
                return Results.BadRequest(new { error = "Unsupported scope" });
            }

            db.StoreUser.Remove(su);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    // ============ OIDC ============
    static void MapOidcEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/api/admin/oidc");

        g.MapGet("", async (PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var items = await db.OidcConfig.OrderByDescending(c => c.UpdatedAt).ToListAsync(ct);
            return Results.Ok(items.Select(c => new OidcConfigDto(
                c.Id, c.ConglomerateId, c.Authority, c.ClientId,
                !string.IsNullOrEmpty(c.ClientSecret),
                c.ResponseType, c.Scopes, c.MapInboundClaims, c.SaveTokens,
                c.GetClaimsFromUserInfoEndpoint, c.ProviderName, c.IsActive, c.UpdatedAt)));
        });

        g.MapPost("", async (UpsertOidcConfigDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var c = new OidcConfig
            {
                ConglomerateId = body.ConglomerateId,
                Authority = body.Authority,
                ClientId = body.ClientId,
                ClientSecret = body.ClientSecret,
                ResponseType = body.ResponseType,
                Scopes = body.Scopes,
                MapInboundClaims = body.MapInboundClaims,
                SaveTokens = body.SaveTokens,
                GetClaimsFromUserInfoEndpoint = body.GetClaimsFromUserInfoEndpoint,
                ProviderName = body.ProviderName,
                IsActive = body.IsActive,
            };
            db.OidcConfig.Add(c);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/admin/oidc/{c.Id}", new { c.Id });
        });

        g.MapPut("/{id}", async (string id, UpsertOidcConfigDto body, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var c = await db.OidcConfig.FindAsync([id], ct);
            if (c is null) return Results.NotFound();
            c.ConglomerateId = body.ConglomerateId;
            c.Authority = body.Authority;
            c.ClientId = body.ClientId;
            if (!string.IsNullOrEmpty(body.ClientSecret)) c.ClientSecret = body.ClientSecret;
            c.ResponseType = body.ResponseType;
            c.Scopes = body.Scopes;
            c.MapInboundClaims = body.MapInboundClaims;
            c.SaveTokens = body.SaveTokens;
            c.GetClaimsFromUserInfoEndpoint = body.GetClaimsFromUserInfoEndpoint;
            c.ProviderName = body.ProviderName;
            c.IsActive = body.IsActive;
            c.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        g.MapDelete("/{id}", async (string id, PermissionGuard guard, IDbContextFactory<BikePosContext> f, CancellationToken ct) =>
        {
            guard.Require("settings.manage");
            using var db = f.CreateDbContext();
            var c = await db.OidcConfig.FindAsync([id], ct);
            if (c is null) return Results.NotFound();
            db.OidcConfig.Remove(c);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
