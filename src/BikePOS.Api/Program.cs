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

// Infrastructure
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IErpAdapter, GenericWebhookAdapter>();
builder.Services.AddScoped<ErpSyncService>();
builder.Services.AddSingleton<SyncTriggerService>();
builder.Services.AddScoped<TicketEventService>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

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

app.MapCustomerEndpoints();
app.MapMechanicEndpoints();
app.MapServiceEndpoints();
app.MapProductEndpoints();
app.MapTicketEndpoints();
app.MapDashboardEndpoints();

app.Run();
