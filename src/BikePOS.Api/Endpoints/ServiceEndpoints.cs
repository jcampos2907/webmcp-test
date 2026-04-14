using BikePOS.Application.Commands;
using BikePOS.Application.Queries;

namespace BikePOS.Api.Endpoints;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/services");

        g.MapGet("", async (ListServicesQueryHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(ct)));

        g.MapGet("/{id}", async (string id, GetServiceByIdQueryHandler h, CancellationToken ct) =>
        {
            var s = await h.HandleAsync(id, ct);
            return s is null ? Results.NotFound() : Results.Ok(s);
        });

        g.MapPost("", async (CreateServiceRequest req, CreateServiceCommandHandler h, CancellationToken ct) =>
        {
            var r = await h.HandleAsync(req, ct);
            return Results.Created($"/api/services/{r.Id}", r);
        });

        g.MapPut("/{id}", async (string id, UpdateServiceRequest req, UpdateServiceCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(req with { Id = id }, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        g.MapDelete("/{id}", async (string id, DeleteServiceCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(new DeleteServiceRequest(id), ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });
    }
}
