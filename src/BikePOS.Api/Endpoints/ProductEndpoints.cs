using BikePOS.Api.Auth;
using BikePOS.Application.Commands;
using BikePOS.Application.Queries;

namespace BikePOS.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/products");

        g.MapGet("", async (ListProductsQueryHandler h, string? search, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(search, ct)));

        g.MapGet("/{id}", async (string id, GetProductByIdQueryHandler h, CancellationToken ct) =>
        {
            var p = await h.HandleAsync(id, ct);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        g.MapPost("", async (CreateProductRequest req, CreateProductCommandHandler h, CancellationToken ct) =>
        {
            var r = await h.HandleAsync(req, ct);
            return Results.Created($"/api/products/{r.Id}", r);
        }).RequireAuthorization(Policies.Admin);

        g.MapPut("/{id}", async (string id, UpdateProductRequest req, UpdateProductCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(req with { Id = id }, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(Policies.Admin);

        g.MapDelete("/{id}", async (string id, DeleteProductCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(new DeleteProductRequest(id), ct);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(Policies.Admin);
    }
}
