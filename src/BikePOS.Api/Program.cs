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
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;

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

const string CorsPolicy = "Frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicy);

// Tenant resolution: read X-Store-Id header and populate TenantContext
app.Use(async (ctx, next) =>
{
    var storeId = ctx.Request.Headers["X-Store-Id"].ToString();
    if (!string.IsNullOrWhiteSpace(storeId))
    {
        var factory = ctx.RequestServices.GetRequiredService<IDbContextFactory<BikePosContext>>();
        await using var db = factory.CreateDbContext();
        // Bypass the tenant wrapper by reading directly (scoped decorator uses same inner factory under filter)
        db.CurrentStoreId = null;
        var store = await db.Store.Include(s => s.Company).ThenInclude(c => c.Conglomerate)
            .FirstOrDefaultAsync(s => s.Id == storeId);
        if (store != null)
        {
            var tenant = ctx.RequestServices.GetRequiredService<TenantContext>();
            tenant.SwitchContext(store.Id, store.Name, store.CompanyId, store.Company.Name, store.Company.ConglomerateId);
        }
    }
    await next();
});

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
