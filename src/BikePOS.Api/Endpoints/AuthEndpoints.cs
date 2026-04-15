using System.Security.Claims;
using BikePOS.Api.Auth;
using BikePOS.Data;
using BikePOS.Models;
using BikePOS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Api.Endpoints;

public static class AuthEndpoints
{
    public record StoreMembershipDto(
        string StoreId, string StoreName,
        string CompanyId, string CompanyName,
        string ConglomerateId, string ConglomerateName,
        string Role, string ViaScope);

    public record MeDto(
        string Id, string? DisplayName, string? Email,
        string? CurrentStoreId, string? CurrentRole,
        string[] Permissions,
        List<StoreMembershipDto> Stores);

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/auth").AllowAnonymous();

        g.MapGet("/login", (string? returnUrl, HttpContext ctx) =>
        {
            var spaBase = app.Configuration["Spa:BaseUrl"]?.TrimEnd('/') ?? "";
            var safeReturn = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            var redirect = safeReturn.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? safeReturn
                : $"{spaBase}{safeReturn}";
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = redirect },
                new[] { OpenIdConnectDefaults.AuthenticationScheme });
        });

        g.MapGet("/logout", async (HttpContext ctx) =>
        {
            var spaBase = app.Configuration["Spa:BaseUrl"]?.TrimEnd('/') ?? "";
            var postLogout = $"{spaBase}/login";
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            try
            {
                await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
                    new AuthenticationProperties { RedirectUri = postLogout });
            }
            catch
            {
                ctx.Response.Redirect(postLogout);
            }
        });

        g.MapGet("/me", async (HttpContext ctx, IDbContextFactory<BikePosContext> f,
                               MembershipResolver resolver, CancellationToken ct) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var appUserId = ctx.User.FindFirstValue("app_user_id");
            if (string.IsNullOrEmpty(appUserId))
                return Results.Unauthorized();

            using var db = f.CreateDbContext();
            db.CurrentStoreId = null;
            var user = await db.AppUser.FirstOrDefaultAsync(u => u.Id == appUserId, ct);
            if (user == null) return Results.Unauthorized();

            var memberships = await resolver.ResolveAsync(appUserId, ct);
            var requested = ctx.Request.Headers["X-Store-Id"].ToString();
            var active = !string.IsNullOrWhiteSpace(requested)
                ? memberships.FirstOrDefault(m => m.StoreId == requested)
                : memberships.FirstOrDefault();

            var stores = memberships.Select(m => new StoreMembershipDto(
                m.StoreId, m.StoreName, m.CompanyId, m.CompanyName,
                m.ConglomerateId, m.ConglomerateName,
                m.Role.ToString(), m.ViaScope.ToString())).ToList();

            return Results.Ok(new MeDto(
                user.Id, user.DisplayName, user.Email,
                active?.StoreId, active?.Role.ToString(),
                active != null ? PermissionsFor(active.Role) : Array.Empty<string>(),
                stores));
        });
    }

    /// <summary>SPA-facing permission flags, delegating to the shared catalog.</summary>
    public static string[] PermissionsFor(StoreRole role) => PermissionCatalog.For(role);
}
