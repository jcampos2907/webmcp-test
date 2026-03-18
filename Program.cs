using BikePOS.Components;
using BikePOS.Data;
using Microsoft.EntityFrameworkCore;
using Blazorise;
using Blazorise.Tailwind;
using Blazorise.Icons.FontAwesome;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using BikePOS.Models;

using Toolbelt.Blazor.Extensions.DependencyInjection;
using BikePOS.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("BikePosContext") ?? throw new InvalidOperationException("Connection string 'BikePosContext' not found.");

builder.Services.AddDbContextFactory<BikePosContext>(options => options.UseSqlite(connectionString));

builder.Services
    .AddBlazorise(options => { options.Immediate = true; })
    .AddTailwindProviders()
    .AddFontAwesomeIcons();

builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



builder.Services.AddHttpContextAccessor();
builder.Services.AddI18nText();
builder.Services.AddScoped<ShopCultureService>();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<AuditDisplayService>();
builder.Services.AddSingleton<SecretProtector>();
builder.Services.AddSingleton<IPaymentTerminalProvider, ManualPaymentProvider>();
builder.Services.AddSingleton<PaymentTerminalService>();

// Decorate IDbContextFactory so every DbContext automatically gets CurrentStoreId from TenantContext
{
    var original = builder.Services.Single(d =>
        d.ServiceType == typeof(IDbContextFactory<BikePosContext>));
    builder.Services.Remove(original);

    // Re-register the EF factory under its concrete type so we can resolve it
    builder.Services.Add(new ServiceDescriptor(
        original.ImplementationType!,
        original.ImplementationType!,
        original.Lifetime));

    builder.Services.AddScoped<IDbContextFactory<BikePosContext>>(sp =>
    {
        var inner = (IDbContextFactory<BikePosContext>)sp.GetRequiredService(original.ImplementationType!);
        var tenant = sp.GetRequiredService<TenantContext>();
        return new TenantDbContextFactory(inner, tenant);
    });
}

// Authentication: OIDC with external IdP
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.ClientId = builder.Configuration["Oidc:ClientId"];
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.MapInboundClaims = false;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = async context =>
        {
            var dbFactory = context.HttpContext.RequestServices
                .GetRequiredService<IDbContextFactory<BikePosContext>>();
            using var db = dbFactory.CreateDbContext();

            var principal = context.Principal!;
            var sub = principal.FindFirstValue("sub") ?? "";
            var name = principal.FindFirstValue("name")
                       ?? principal.FindFirstValue("preferred_username");
            var email = principal.FindFirstValue("email");

            // Upsert AppUser
            var appUser = await db.AppUser.FirstOrDefaultAsync(u => u.ExternalSubjectId == sub);
            if (appUser == null)
            {
                appUser = new AppUser { ExternalSubjectId = sub, DisplayName = name, Email = email };
                db.AppUser.Add(appUser);
            }
            else
            {
                appUser.DisplayName = name;
                appUser.Email = email;
                appUser.LastLoginAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();

            // Check if IdP assigns this user a superadmin role (for bootstrap/setup)
            var idpRoles = principal.FindAll("roles").Select(c => c.Value)
                .Concat(principal.FindAll("role").Select(c => c.Value))
                .Select(r => r.ToLowerInvariant())
                .ToHashSet();
            var isIdpSuperAdmin = idpRoles.Contains("superadmin") || idpRoles.Contains("super_admin");

            // Find store assignment
            var storeUser = await db.StoreUser
                .Include(su => su.Store).ThenInclude(s => s.Company)
                .Where(su => su.AppUserId == appUser.Id)
                .FirstOrDefaultAsync();

            // Auto-assign SuperAdmin if: IdP says superadmin, OR first user ever
            if (storeUser == null && (isIdpSuperAdmin || !await db.StoreUser.AnyAsync()))
            {
                var defaultStore = await db.Store.Include(s => s.Company).FirstOrDefaultAsync();
                if (defaultStore != null)
                {
                    storeUser = new StoreUser
                    {
                        AppUserId = appUser.Id,
                        StoreId = defaultStore.Id,
                        Role = StoreRole.SuperAdmin
                    };
                    db.StoreUser.Add(storeUser);
                    await db.SaveChangesAsync();
                    storeUser.Store = defaultStore;
                }
            }

            // Add tenant claims to the cookie identity
            if (storeUser != null)
            {
                var identity = (ClaimsIdentity)principal.Identity!;
                identity.AddClaim(new Claim("app_user_id", appUser.Id.ToString()));
                identity.AddClaim(new Claim("store_id", storeUser.StoreId.ToString()));
                identity.AddClaim(new Claim("store_name", storeUser.Store.Name));
                identity.AddClaim(new Claim("company_id", storeUser.Store.CompanyId.ToString()));
                identity.AddClaim(new Claim("company_name", storeUser.Store.Company.Name));
                identity.AddClaim(new Claim("conglomerate_id", storeUser.Store.Company.ConglomerateId.ToString()));
                identity.AddClaim(new Claim("store_role", storeUser.Role.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Role, storeUser.Role.ToString()));
            }
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddOpenApi();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    SeedData.Initialize(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseMigrationsEndPoint();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
;
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Auth endpoints — OIDC requires full HTTP round-trips, not Blazor components
app.MapGet("/account/login", (string? returnUrl) =>
    Results.Challenge(new AuthenticationProperties
    {
        RedirectUri = returnUrl ?? "/"
    })).AllowAnonymous();

app.MapGet("/account/logout", async (HttpContext httpContext) =>
{
    // Always clear the local cookie
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    try
    {
        // Try OIDC sign-out (requires IdP to be reachable)
        await httpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    }
    catch
    {
        // IdP unreachable — local cookie is already cleared, redirect home
        httpContext.Response.Redirect("/");
    }
}).AllowAnonymous();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal API endpoints for WebMCP tools (anonymous for now — secure in Step 3)
var componentApi = app.MapGroup("/api/components").AllowAnonymous();

componentApi.MapGet("/", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.Component.ToListAsync());
});

componentApi.MapGet("/{id}", async (string id, IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var component = await context.Component.FindAsync(id);
    return component is not null ? Results.Ok(component) : Results.NotFound();
});

componentApi.MapGet("/search", async (string? query, IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var components = context.Component.AsQueryable();
    if (!string.IsNullOrWhiteSpace(query))
    {
        components = components.Where(c =>
            (c.Name != null && c.Name.Contains(query)) ||
            c.Brand.Contains(query) ||
            c.Color.Contains(query) ||
            c.Sku.Contains(query));
    }
    return Results.Ok(await components.ToListAsync());
});

// Ticket API endpoints
var ticketApi = app.MapGroup("/api/tickets").AllowAnonymous();

ticketApi.MapGet("/", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.ServiceTicket
        .Include(t => t.Component)
        .Include(t => t.Mechanic)
        .Include(t => t.BaseService)
        .ToListAsync());
});

ticketApi.MapGet("/{id}", async (string id, IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var ticket = await context.ServiceTicket
        .Include(t => t.Component)
        .Include(t => t.Mechanic)
        .Include(t => t.BaseService)
        .Include(t => t.TicketProducts).ThenInclude(tp => tp.Product)
        .FirstOrDefaultAsync(t => t.Id == id);
    return ticket is not null ? Results.Ok(ticket) : Results.NotFound();
});

ticketApi.MapGet("/search", async (string? status, string? componentId, string? mechanicId, IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var tickets = context.ServiceTicket
        .Include(t => t.Component)
        .Include(t => t.Mechanic)
        .AsQueryable();

    if (Enum.TryParse<BikePOS.Models.TicketStatus>(status, true, out var ticketStatus))
        tickets = tickets.Where(t => t.Status == ticketStatus);
    if (!string.IsNullOrEmpty(componentId))
        tickets = tickets.Where(t => t.ComponentId == componentId);
    if (!string.IsNullOrEmpty(mechanicId))
        tickets = tickets.Where(t => t.MechanicId == mechanicId);

    return Results.Ok(await tickets.ToListAsync());
});

ticketApi.MapGet("/open", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var openStatuses = new[] { BikePOS.Models.TicketStatus.Open, BikePOS.Models.TicketStatus.InProgress, BikePOS.Models.TicketStatus.WaitingForParts };
    return Results.Ok(await context.ServiceTicket
        .Include(t => t.Component)
        .Include(t => t.Mechanic)
        .Where(t => openStatuses.Contains(t.Status))
        .OrderByDescending(t => t.CreatedAt)
        .ToListAsync());
});

// Mechanic API endpoints
var mechanicApi = app.MapGroup("/api/mechanics").AllowAnonymous();

mechanicApi.MapGet("/", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.Mechanic.ToListAsync());
});

// Service API endpoints
var serviceApi = app.MapGroup("/api/services").AllowAnonymous();

serviceApi.MapGet("/", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.Service.ToListAsync());
});

// Product API endpoints
var productApi = app.MapGroup("/api/products").AllowAnonymous();

productApi.MapGet("/", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.Product.ToListAsync());
});

productApi.MapGet("/search", async (string? query, IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var products = context.Product.AsQueryable();
    if (!string.IsNullOrWhiteSpace(query))
    {
        products = products.Where(p =>
            p.Name.Contains(query) ||
            (p.Sku != null && p.Sku.Contains(query)) ||
            (p.Category != null && p.Category.Contains(query)));
    }
    return Results.Ok(await products.ToListAsync());
});

app.MapChargeEndpoints();

app.Run();