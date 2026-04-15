using BikePOS.Api.Auth;
using BikePOS.Application.Commands;
using BikePOS.Application.Queries;

namespace BikePOS.Api.Endpoints;

public static class MechanicEndpoints
{
    public static void MapMechanicEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/mechanics");

        g.MapGet("", async (ListMechanicsQueryHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(ct)));

        g.MapGet("/{id}", async (string id, GetMechanicByIdQueryHandler h, CancellationToken ct) =>
        {
            var m = await h.HandleAsync(id, ct);
            return m is null ? Results.NotFound() : Results.Ok(m);
        });

        g.MapGet("/workload", async (GetMechanicWorkloadQueryHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(ct)));

        g.MapPost("", async (CreateMechanicRequest req, CreateMechanicCommandHandler h, CancellationToken ct) =>
        {
            var r = await h.HandleAsync(req, ct);
            return Results.Created($"/api/mechanics/{r.Id}", r);
        }).RequireAuthorization(Policies.Admin);

        g.MapPut("/{id}", async (string id, UpdateMechanicRequest req, UpdateMechanicCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(req with { Id = id }, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(Policies.Admin);

        g.MapDelete("/{id}", async (string id, DeleteMechanicCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(new DeleteMechanicRequest(id), ct);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(Policies.Admin);
    }
}
