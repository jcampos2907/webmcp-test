using System.Security.Claims;
using BikePOS.Api.Auth;
using BikePOS.Api.Endpoints;
using BikePOS.Application.Commands;
using BikePOS.Application.EventHandlers;
using BikePOS.Application.Queries;
using BikePOS.Data;
using BikePOS.Domain.Aggregates.Customer.Events;
using BikePOS.Domain.Aggregates.Inventory.Events;
using BikePOS.Domain.Aggregates.ServiceTicket.Events;
using BikePOS.Infrastructure;
using BikePOS.Infrastructure.Erp;
using BikePOS.Infrastructure.Payments;
using BikePOS.Infrastructure.Persistence;
using BikePOS.Interfaces.Events;
using BikePOS.Interfaces.Repositories;
using BikePOS.Interfaces.Services;
using BikePOS.Models;
using BikePOS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BikePosContext")
    ?? throw new InvalidOperationException("Connection string 'BikePosContext' not found.");

builder.Services.AddDbContextFactory<BikePosContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("BikePOS.Web")));

// Tenant scoping: TenantContext + decorated IDbContextFactory so every DbContext gets CurrentStoreId
builder.Services.AddScoped<TenantContext>();
{
    var original = builder.Services.Single(d =>
        d.ServiceType == typeof(IDbContextFactory<BikePosContext>));
    builder.Services.Remove(original);
    builder.Services.Add(new ServiceDescriptor(
        original.ImplementationType!, original.ImplementationType!, original.Lifetime));
    builder.Services.AddScoped<IDbContextFactory<BikePosContext>>(sp =>
    {
        var inner = (IDbContextFactory<BikePosContext>)sp.GetRequiredService(original.ImplementationType!);
        var tenant = sp.GetRequiredService<TenantContext>();
        return new TenantDbContextFactory(inner, tenant);
    });
}

// Infrastructure
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IErpAdapter, GenericWebhookAdapter>();
builder.Services.AddScoped<ErpSyncService>();
builder.Services.AddSingleton<SyncTriggerService>();
builder.Services.AddScoped<TicketEventService>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// Payment
builder.Services.AddSingleton<IPaymentTerminalProvider, ManualPaymentProvider>();
var simulateTerminals = builder.Configuration.GetValue<bool>("SimulateTerminals", true);
if (simulateTerminals)
{
    builder.Services.AddSingleton<IPaymentTerminalProvider>(new SimulatedPaymentProvider(BikePOS.Models.TerminalProvider.Ingenico));
    builder.Services.AddSingleton<IPaymentTerminalProvider>(new SimulatedPaymentProvider(BikePOS.Models.TerminalProvider.Verifone));
    builder.Services.AddSingleton<IPaymentTerminalProvider>(new SimulatedPaymentProvider(BikePOS.Models.TerminalProvider.PAX));
    builder.Services.AddSingleton<IPaymentTerminalProvider>(new SimulatedPaymentProvider(BikePOS.Models.TerminalProvider.Nexgo));
}
else
{
    builder.Services.AddSingleton<IPaymentTerminalProvider, IngenicoPaymentProvider>();
}
builder.Services.AddSingleton<PaymentTerminalService>();

// Repositories
builder.Services.AddScoped<IServiceTicketRepository, ServiceTicketRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddScoped<IChargeRepository, ChargeRepository>();
builder.Services.AddScoped<IMechanicRepository, MechanicRepository>();

// Command handlers
builder.Services.AddScoped<CreateTicketCommandHandler>();
builder.Services.AddScoped<CancelTicketCommandHandler>();
builder.Services.AddScoped<ProcessChargeCommandHandler>();
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

// Query handlers
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

// Domain event handlers (registered so dispatcher finds them; LowStock/etc rely on notification stack, skip those)
builder.Services.AddScoped<IDomainEventHandler<CustomerCreatedEvent>, ErpCustomerCreatedHandler>();
builder.Services.AddScoped<IDomainEventHandler<TicketCreatedEvent>, ErpTicketCreatedHandler>();
builder.Services.AddScoped<IDomainEventHandler<TicketStatusChangedEvent>, ErpTicketStatusChangedHandler>();

builder.Services.AddOpenApi();

// ---- Authentication: cookie + Keycloak OIDC (BFF pattern) ----
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        // Default challenge = cookie → returns 401 for API callers.
        // Explicit OIDC challenge is used by /api/auth/login for the interactive flow.
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "bikepos.auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        // Return 401 instead of 302 to /Account/Login for API callers
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = builder.Configuration["Oidc:Authority"];
        options.ClientId = builder.Configuration["Oidc:ClientId"];
        options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = false; // keep cookie small; tokens not needed for v1 BFF
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        // Route callbacks under /api/* so the Vite dev proxy forwards them
        options.CallbackPath = "/api/auth/signin-oidc";
        options.SignedOutCallbackPath = "/api/auth/signout-callback-oidc";
        options.RemoteSignOutPath = "/api/auth/signout-oidc";

        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                var dbFactory = context.HttpContext.RequestServices
                    .GetRequiredService<IDbContextFactory<BikePosContext>>();
                await using var db = dbFactory.CreateDbContext();
                db.CurrentStoreId = null; // bypass tenant filter

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

                // Bootstrap: first user ever, or IdP marks them SuperAdmin → conglomerate-wide SuperAdmin
                var idpRoles = principal.FindAll("roles").Select(c => c.Value)
                    .Concat(principal.FindAll("role").Select(c => c.Value))
                    .Select(r => r.ToLowerInvariant())
                    .ToHashSet();
                var isIdpSuperAdmin = idpRoles.Contains("superadmin") || idpRoles.Contains("super_admin");

                var hasAnyAssignment = await db.StoreUser.AnyAsync(su => su.AppUserId == appUser.Id);
                if (!hasAnyAssignment && (isIdpSuperAdmin || !await db.StoreUser.AnyAsync()))
                {
                    var defaultStore = await db.Store.Include(s => s.Company).FirstOrDefaultAsync();
                    if (defaultStore != null)
                    {
                        db.StoreUser.Add(new StoreUser
                        {
                            AppUserId = appUser.Id,
                            Scope = RoleScope.Conglomerate,
                            ConglomerateId = defaultStore.Company.ConglomerateId,
                            Role = StoreRole.SuperAdmin
                        });
                        await db.SaveChangesAsync();
                    }
                }

                var identity = (ClaimsIdentity)principal.Identity!;
                identity.AddClaim(new Claim("app_user_id", appUser.Id));
                // Store/Company/Role claims are populated per-request by tenant middleware (they depend on the active store).
            },
            OnRemoteFailure = context =>
            {
                // Don't crash the app when IdP login is cancelled or errors — send user back to /login
                var spaBase = builder.Configuration["Spa:BaseUrl"] ?? "/";
                context.Response.Redirect($"{spaBase.TrimEnd('/')}/login?error=oidc");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<MembershipResolver>();
builder.Services.AddScoped<IAuthorizationHandler, MinRoleHandler>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    Policies.Register(options);
});

const string CorsPolicy = "Frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, p => p
        .WithOrigins(builder.Configuration["Spa:BaseUrl"] ?? "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicy);
app.UseAuthentication();

// Tenant resolution: every authenticated request resolves the active store the user can
// operate on. Rules:
//   - X-Store-Id header, if present, must be a store the user has an effective membership for
//     (Store / Company / Conglomerate scope). Otherwise 403.
//   - No header → pick the first accessible store deterministically.
//   - The effective role for that store (highest-ranked covering assignment) is written to
//     TenantContext.Role and is what policy handlers check.
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    if (ctx.User.Identity?.IsAuthenticated == true && !path.StartsWith("/api/auth/"))
    {
        var tenant = ctx.RequestServices.GetRequiredService<TenantContext>();
        var resolver = ctx.RequestServices.GetRequiredService<MembershipResolver>();
        var appUserId = ctx.User.FindFirstValue("app_user_id");
        if (string.IsNullOrEmpty(appUserId))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var memberships = await resolver.ResolveAsync(appUserId);
        if (memberships.Count == 0)
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsync("No store memberships");
            return;
        }

        var requestedStoreId = ctx.Request.Headers["X-Store-Id"].ToString();
        EffectiveMembership? active;
        if (!string.IsNullOrWhiteSpace(requestedStoreId))
        {
            active = memberships.FirstOrDefault(m => m.StoreId == requestedStoreId);
            if (active == null)
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsync("Not a member of requested store");
                return;
            }
        }
        else
        {
            active = memberships[0];
        }

        tenant.PopulateFromClaims(ctx.User);
        tenant.SwitchContext(active.StoreId, active.StoreName, active.CompanyId, active.CompanyName, active.ConglomerateId);
        tenant.SetRole(active.Role);
    }
    await next();
});

app.UseAuthorization();

// Auth endpoints (anonymous — allow login flow before user is authenticated)
app.MapAuthEndpoints();

// FallbackPolicy above already requires authenticated user for everything else
app.MapCustomerEndpoints();
app.MapMechanicEndpoints();
app.MapServiceEndpoints();
app.MapProductEndpoints();
app.MapTicketEndpoints();
app.MapDashboardEndpoints();
app.MapComponentEndpoints();
app.MapMetaFieldEndpoints();
app.MapReportEndpoints();
app.MapSettingsEndpoints();
app.MapAdminEndpoints();
app.MapSessionEndpoints();
app.MapTerminalPublicEndpoints();

app.Run();
