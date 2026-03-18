using BikePOS.Components;
using BikePOS.Data;
using Microsoft.EntityFrameworkCore;
using Blazorise;
using Blazorise.Tailwind;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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



builder.Services.AddI18nText();
builder.Services.AddScoped<ShopCultureService>();

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
});

builder.Services.AddAuthorization();
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
    }));

app.MapGet("/account/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal API endpoints for WebMCP tools
var componentApi = app.MapGroup("/api/components");

componentApi.MapGet("/", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.Component.ToListAsync());
});

componentApi.MapGet("/{id:int}", async (int id, IDbContextFactory<BikePosContext> dbFactory) =>
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
var ticketApi = app.MapGroup("/api/tickets");

ticketApi.MapGet("/", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.ServiceTicket
        .Include(t => t.Component)
        .Include(t => t.Mechanic)
        .Include(t => t.BaseService)
        .ToListAsync());
});

ticketApi.MapGet("/{id:int}", async (int id, IDbContextFactory<BikePosContext> dbFactory) =>
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

ticketApi.MapGet("/search", async (string? status, int? componentId, int? mechanicId, IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var tickets = context.ServiceTicket
        .Include(t => t.Component)
        .Include(t => t.Mechanic)
        .AsQueryable();

    if (Enum.TryParse<BikePOS.Models.TicketStatus>(status, true, out var ticketStatus))
        tickets = tickets.Where(t => t.Status == ticketStatus);
    if (componentId.HasValue)
        tickets = tickets.Where(t => t.ComponentId == componentId.Value);
    if (mechanicId.HasValue)
        tickets = tickets.Where(t => t.MechanicId == mechanicId.Value);

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
var mechanicApi = app.MapGroup("/api/mechanics");

mechanicApi.MapGet("/", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.Mechanic.ToListAsync());
});

// Service API endpoints
var serviceApi = app.MapGroup("/api/services");

serviceApi.MapGet("/", async (IDbContextFactory<BikePosContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.Service.ToListAsync());
});

// Product API endpoints
var productApi = app.MapGroup("/api/products");

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