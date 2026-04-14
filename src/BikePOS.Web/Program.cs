using BikePOS.Components;
using BikePOS.Data;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using BikePOS.Models;

using Toolbelt.Blazor.Extensions.DependencyInjection;
using BikePOS.Services;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Interfaces.Repositories;
using BikePOS.Infrastructure;
using BikePOS.Infrastructure.Persistence;
using BikePOS.Infrastructure.Erp;
using BikePOS.Application.Commands;
using BikePOS.Application.Queries;
using BikePOS.Application.EventHandlers;
using BikePOS.Domain.Aggregates.ServiceTicket.Events;
using BikePOS.Domain.Aggregates.Customer.Events;
using BikePOS.Domain.Aggregates.Inventory.Events;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("BikePosContext") ?? throw new InvalidOperationException("Connection string 'BikePosContext' not found.");

builder.Services.AddDbContextFactory<BikePosContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("BikePOS.Web")));

builder.Services.AddMudServices();

builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



builder.Services.AddHttpContextAccessor();
builder.Services.AddI18nText(options =>
{
    options.GetInitialLanguageAsync = (serviceProvider, _) =>
    {
        var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        var request = httpContextAccessor?.HttpContext?.Request;

        // 1) Cookie (set by Settings page on language change)
        var lang = request?.Cookies["lang"];

        // 2) Fallback: Accept-Language header (first visit)
        if (string.IsNullOrEmpty(lang))
        {
            var acceptLang = request?.Headers["Accept-Language"].ToString();
            if (!string.IsNullOrEmpty(acceptLang))
            {
                var primary = acceptLang.Split(',').FirstOrDefault()?.Trim().Split(';').FirstOrDefault();
                lang = primary?.Split('-').FirstOrDefault();
            }
        }

        // 3) Default to Spanish
        var supported = new HashSet<string> { "en", "es" };
        if (string.IsNullOrEmpty(lang) || !supported.Contains(lang))
            lang = "es";

        return ValueTask.FromResult(lang);
    };
});
builder.Services.AddScoped<ShopCultureService>();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<AuditDisplayService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<BikePOS.Infrastructure.Notifications.NotificationService>();
builder.Services.AddScoped<TicketEventService>();
builder.Services.AddHttpClient("ErpWebhook");
builder.Services.AddSingleton<BikePOS.Interfaces.Services.IErpAdapter, BikePOS.Infrastructure.Erp.GenericWebhookAdapter>();
builder.Services.AddScoped<BikePOS.Infrastructure.Erp.ErpSyncService>();
builder.Services.AddSingleton<BikePOS.Infrastructure.Erp.SyncTriggerService>();
builder.Services.AddSingleton<SecretProtector>();
builder.Services.AddSingleton<BikePOS.Interfaces.Services.IPaymentTerminalProvider, BikePOS.Infrastructure.Payments.ManualPaymentProvider>();
var simulateTerminals = builder.Configuration.GetValue<bool>("SimulateTerminals");
if (simulateTerminals)
{
    builder.Services.AddSingleton<BikePOS.Interfaces.Services.IPaymentTerminalProvider>(
        new BikePOS.Infrastructure.Payments.SimulatedPaymentProvider(BikePOS.Models.TerminalProvider.Ingenico));
    builder.Services.AddSingleton<BikePOS.Interfaces.Services.IPaymentTerminalProvider>(
        new BikePOS.Infrastructure.Payments.SimulatedPaymentProvider(BikePOS.Models.TerminalProvider.Verifone));
    builder.Services.AddSingleton<BikePOS.Interfaces.Services.IPaymentTerminalProvider>(
        new BikePOS.Infrastructure.Payments.SimulatedPaymentProvider(BikePOS.Models.TerminalProvider.PAX));
    builder.Services.AddSingleton<BikePOS.Interfaces.Services.IPaymentTerminalProvider>(
        new BikePOS.Infrastructure.Payments.SimulatedPaymentProvider(BikePOS.Models.TerminalProvider.Nexgo));
}
else
{
    builder.Services.AddSingleton<BikePOS.Interfaces.Services.IPaymentTerminalProvider, BikePOS.Infrastructure.Payments.IngenicoPaymentProvider>();
}
builder.Services.AddSingleton<BikePOS.Infrastructure.Payments.PaymentTerminalService>();

// Receipt Printers
if (simulateTerminals)
{
    builder.Services.AddSingleton<BikePOS.Interfaces.Services.IReceiptPrinterProvider, BikePOS.Infrastructure.Printing.SimulatedReceiptProvider>();
}
else
{
    builder.Services.AddSingleton<BikePOS.Interfaces.Services.IReceiptPrinterProvider, BikePOS.Infrastructure.Printing.EscPosReceiptProvider>();
}
builder.Services.AddSingleton<BikePOS.Infrastructure.Printing.ReceiptPrinterService>();

// DDD: Domain Event Dispatcher
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// DDD: Repository implementations
builder.Services.AddScoped<IServiceTicketRepository, ServiceTicketRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddScoped<IChargeRepository, ChargeRepository>();
builder.Services.AddScoped<IMechanicRepository, MechanicRepository>();

// DDD: Application Command Handlers
builder.Services.AddScoped<CreateTicketCommandHandler>();
builder.Services.AddScoped<ProcessChargeCommandHandler>();
builder.Services.AddScoped<PrintReceiptCommandHandler>();
builder.Services.AddScoped<PrintServiceTagCommandHandler>();
builder.Services.AddScoped<CancelTicketCommandHandler>();
builder.Services.AddScoped<ProcessRefundCommandHandler>();
builder.Services.AddScoped<CreateServiceCommandHandler>();
builder.Services.AddScoped<UpdateServiceCommandHandler>();
builder.Services.AddScoped<DeleteServiceCommandHandler>();
builder.Services.AddScoped<CreateMechanicCommandHandler>();
builder.Services.AddScoped<UpdateMechanicCommandHandler>();
builder.Services.AddScoped<DeleteMechanicCommandHandler>();
builder.Services.AddScoped<CreateProductCommandHandler>();
builder.Services.AddScoped<UpdateProductCommandHandler>();
builder.Services.AddScoped<DeleteProductCommandHandler>();
builder.Services.AddScoped<CreateCustomerCommandHandler>();
builder.Services.AddScoped<UpdateCustomerCommandHandler>();
builder.Services.AddScoped<DeleteCustomerCommandHandler>();

// DDD: Application Query Handlers
builder.Services.AddScoped<GetTicketDetailsQueryHandler>();
builder.Services.AddScoped<DailySalesQueryHandler>();
builder.Services.AddScoped<ListServicesQueryHandler>();
builder.Services.AddScoped<GetServiceByIdQueryHandler>();
builder.Services.AddScoped<ListMechanicsQueryHandler>();
builder.Services.AddScoped<GetMechanicByIdQueryHandler>();
builder.Services.AddScoped<GetMechanicWorkloadQueryHandler>();
builder.Services.AddScoped<ListProductsQueryHandler>();
builder.Services.AddScoped<GetProductByIdQueryHandler>();
builder.Services.AddScoped<ListCustomersQueryHandler>();
builder.Services.AddScoped<GetCustomerByIdQueryHandler>();
builder.Services.AddScoped<ListTicketsQueryHandler>();
builder.Services.AddScoped<GetTicketByIdQueryHandler>();
builder.Services.AddScoped<SearchTicketsQueryHandler>();
builder.Services.AddScoped<ListMetaFieldsQueryHandler>();
builder.Services.AddScoped<LoadBaseFieldLayoutsQueryHandler>();
builder.Services.AddScoped<LoadEntityMetaValuesQueryHandler>();
builder.Services.AddScoped<LoadCustomerMetaValuesQueryHandler>();
builder.Services.AddScoped<SaveEntityMetaValuesCommandHandler>();
builder.Services.AddScoped<SaveCustomerMetaValuesCommandHandler>();
builder.Services.AddScoped<ApplyCountryPresetsCommandHandler>();

// DDD: Domain Event Handlers
builder.Services.AddScoped<IDomainEventHandler<TicketChargedEvent>, TicketChargedEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<TicketStatusChangedEvent>, TicketStatusChangedEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<LowStockEvent>, LowStockEventHandler>();
builder.Services.AddScoped<IDomainEventHandler<CustomerCreatedEvent>, CustomerCreatedEventHandler>();

// DDD: ERP Anti-Corruption Layer event handlers
builder.Services.AddScoped<IDomainEventHandler<TicketCreatedEvent>, ErpTicketCreatedHandler>();
builder.Services.AddScoped<IDomainEventHandler<TicketStatusChangedEvent>, ErpTicketStatusChangedHandler>();
builder.Services.AddScoped<IDomainEventHandler<CustomerCreatedEvent>, ErpCustomerCreatedHandler>();

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

// Authentication: dev bypass or OIDC with external IdP
var devBypassAuth = builder.Configuration.GetValue<bool>("DevBypassAuth");
if (devBypassAuth)
{
    builder.Services.AddAuthentication("DevBypass")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevBypassAuthHandler>("DevBypass", null);
}
else
{
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
}

builder.Services.AddAuthorization(options =>
{
    if (!devBypassAuth)
    {
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
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

// ERP inbound webhook endpoint
app.MapPost("/api/erp/webhook/{connectionId}", async (
    string connectionId,
    HttpRequest request,
    IDbContextFactory<BikePOS.Data.BikePosContext> dbFactory) =>
{
    using var db = dbFactory.CreateDbContext();
    var conn = await db.ErpConnection
        .Include(c => c.FieldMappings)
        .FirstOrDefaultAsync(c => c.Id == connectionId && c.IsActive);

    if (conn == null) return Results.NotFound("Connection not found or inactive");

    // Validate API key from Authorization header
    var authHeader = request.Headers.Authorization.ToString();
    if (!string.IsNullOrEmpty(conn.ApiKey))
    {
        var token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (token != conn.ApiKey) return Results.Unauthorized();
    }

    // Parse body
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    System.Text.Json.JsonElement payload;
    try { payload = System.Text.Json.JsonDocument.Parse(body).RootElement; }
    catch { return Results.BadRequest("Invalid JSON"); }

    if (!payload.TryGetProperty("entity_type", out var entityTypeProp))
        return Results.BadRequest("Missing entity_type");
    var entityType = entityTypeProp.GetString()!;
    var externalId = payload.TryGetProperty("external_id", out var extIdProp) ? extIdProp.GetString() : null;

    if (string.IsNullOrEmpty(externalId))
        return Results.BadRequest("Missing external_id");

    // Check entity sync is enabled
    var enabled = entityType switch
    {
        "Customer" => conn.SyncCustomers,
        "Component" => conn.SyncComponents,
        "Product" => conn.SyncProducts,
        "ServiceTicket" => conn.SyncTickets,
        "Charge" => conn.SyncCharges,
        _ => false
    };
    if (!enabled) return Results.BadRequest($"Sync not enabled for {entityType}");

    var log = new BikePOS.Models.SyncLog
    {
        ErpConnectionId = conn.Id,
        Direction = BikePOS.Models.SyncDirection.Inbound,
        EntityType = entityType,
        StoreId = conn.StoreId,
        RequestPayload = body
    };
    db.SyncLog.Add(log);

    try
    {
        var fields = payload.TryGetProperty("fields", out var fieldsProp) ? fieldsProp : payload;
        var mappings = conn.FieldMappings.Where(m => m.EntityType == entityType).ToList();

        // Resolve mapped field values
        var mapped = new Dictionary<string, string?>();
        if (mappings.Count > 0)
        {
            foreach (var m in mappings)
            {
                if (fields.TryGetProperty(m.RemoteField, out var val))
                    mapped[m.LocalField] = val.ValueKind == System.Text.Json.JsonValueKind.Null ? null : val.ToString();
            }
        }
        else
        {
            // No mappings — use field names directly
            foreach (var prop in fields.EnumerateObject())
            {
                if (prop.Name is "entity_type" or "external_id" or "action") continue;
                mapped[prop.Name] = prop.Value.ValueKind == System.Text.Json.JsonValueKind.Null ? null : prop.Value.ToString();
            }
        }

        // Find or create entity
        switch (entityType)
        {
            case "Customer":
                var customer = await db.Customer.FirstOrDefaultAsync(c => c.ExternalId == externalId && c.ExternalSource == conn.Provider);
                if (customer == null)
                {
                    customer = new BikePOS.Models.Customer { StoreId = conn.StoreId, ExternalId = externalId, ExternalSource = conn.Provider };
                    db.Customer.Add(customer);
                }
                ApplyFields(customer, mapped);
                log.EntityId = customer.Id;
                break;

            case "Product":
                var product = await db.Product.FirstOrDefaultAsync(p => p.ExternalId == externalId && p.ExternalSource == conn.Provider);
                if (product == null)
                {
                    product = new BikePOS.Models.Product { StoreId = conn.StoreId, ExternalId = externalId, ExternalSource = conn.Provider, Name = "Imported" };
                    db.Product.Add(product);
                }
                ApplyFields(product, mapped);
                log.EntityId = product.Id;
                break;

            case "Component":
                var component = await db.Component.FirstOrDefaultAsync(c => c.ExternalId == externalId && c.ExternalSource == conn.Provider);
                if (component == null)
                {
                    component = new BikePOS.Models.Component { StoreId = conn.StoreId, ExternalId = externalId, ExternalSource = conn.Provider, Sku = "IMPORTED", Color = "", Brand = "" };
                    db.Component.Add(component);
                }
                ApplyFields(component, mapped);
                log.EntityId = component.Id;
                break;

            default:
                log.Status = BikePOS.Models.SyncStatus.Skipped;
                log.ErrorMessage = $"Inbound sync not supported for {entityType}";
                await db.SaveChangesAsync();
                return Results.Ok(new { status = "skipped", reason = log.ErrorMessage });
        }

        await db.SaveChangesAsync();
        log.Status = BikePOS.Models.SyncStatus.Success;
        log.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { status = "ok", entity_id = log.EntityId });
    }
    catch (Exception ex)
    {
        log.Status = BikePOS.Models.SyncStatus.Failed;
        log.ErrorMessage = ex.Message;
        log.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.StatusCode(500);
    }
}).AllowAnonymous();

// Helper to apply mapped fields to an entity via reflection
static void ApplyFields(object entity, Dictionary<string, string?> fields)
{
    var props = entity.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
    foreach (var (key, value) in fields)
    {
        var prop = props.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
        if (prop == null || !prop.CanWrite) continue;
        if (prop.Name is "Id" or "StoreId" or "ExternalId" or "ExternalSource") continue; // protect key fields

        try
        {
            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (value == null)
                prop.SetValue(entity, null);
            else if (targetType == typeof(decimal))
                prop.SetValue(entity, decimal.Parse(value));
            else if (targetType == typeof(int))
                prop.SetValue(entity, int.Parse(value));
            else if (targetType == typeof(bool))
                prop.SetValue(entity, bool.Parse(value));
            else if (targetType == typeof(DateTime))
                prop.SetValue(entity, DateTime.Parse(value));
            else
                prop.SetValue(entity, value);
        }
        catch { /* skip fields that can't be converted */ }
    }
}

app.Run();

public class DevBypassAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public DevBypassAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        IDbContextFactory<BikePosContext> dbFactory) : base(options, logger, encoder)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        var store = await db.Store.Include(s => s.Company).FirstOrDefaultAsync();

        var claims = new List<Claim>
        {
            new Claim("sub", "dev-user"),
            new Claim("name", "Dev Admin"),
            new Claim("email", "dev@bikepos.local"),
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            new Claim("store_role", "SuperAdmin"),
        };

        if (store != null)
        {
            claims.Add(new Claim("store_id", store.Id.ToString()));
            claims.Add(new Claim("store_name", store.Name));
            claims.Add(new Claim("company_id", store.CompanyId.ToString()));
            claims.Add(new Claim("company_name", store.Company.Name));
            claims.Add(new Claim("conglomerate_id", store.Company.ConglomerateId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, Scheme.Name);
        return Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket);
    }
}