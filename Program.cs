using webmcp.Components;
using webmcp.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("webmcpContext") ?? throw new InvalidOperationException("Connection string 'webmcpContext' not found.");

builder.Services.AddDbContextFactory<webmcpContext>(options => options.UseSqlite(connectionString));

builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal API endpoints for WebMCP tools
var bikeApi = app.MapGroup("/api/bikes");

bikeApi.MapGet("/", async (IDbContextFactory<webmcpContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    return Results.Ok(await context.Bike.ToListAsync());
});

bikeApi.MapGet("/{id:int}", async (int id, IDbContextFactory<webmcpContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var bike = await context.Bike.FindAsync(id);
    return bike is not null ? Results.Ok(bike) : Results.NotFound();
});

bikeApi.MapGet("/search", async (string? query, IDbContextFactory<webmcpContext> dbFactory) =>
{
    using var context = dbFactory.CreateDbContext();
    var bikes = context.Bike.AsQueryable();
    if (!string.IsNullOrWhiteSpace(query))
    {
        bikes = bikes.Where(b =>
            (b.Name != null && b.Name.Contains(query)) ||
            b.Brand.Contains(query) ||
            b.Color.Contains(query) ||
            b.Sku.Contains(query));
    }
    return Results.Ok(await bikes.ToListAsync());
});

app.Run();
