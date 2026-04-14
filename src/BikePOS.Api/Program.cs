using BikePOS.Application.Queries;
using BikePOS.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BikePosContext")
    ?? throw new InvalidOperationException("Connection string 'BikePosContext' not found.");

builder.Services.AddDbContextFactory<BikePosContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("BikePOS.Web")));

builder.Services.AddScoped<ListCustomersQueryHandler>();
builder.Services.AddScoped<GetCustomerByIdQueryHandler>();

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

app.MapGet("/api/customers", async (ListCustomersQueryHandler handler, string? search, CancellationToken ct) =>
{
    var customers = await handler.HandleAsync(search, ct);
    return Results.Ok(customers.Select(c => new CustomerListDto(
        c.Id, c.FirstName, c.LastName, $"{c.FirstName} {c.LastName}".Trim(), c.Phone, c.Email, c.City)));
});

app.MapGet("/api/customers/{id}", async (string id, GetCustomerByIdQueryHandler handler, CancellationToken ct) =>
{
    var customer = await handler.HandleAsync(id, includeComponents: true, ct);
    return customer is null ? Results.NotFound() : Results.Ok(customer);
});

app.Run();
