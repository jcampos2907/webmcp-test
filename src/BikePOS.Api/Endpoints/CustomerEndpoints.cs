using BikePOS.Application.Commands;
using BikePOS.Application.Queries;

namespace BikePOS.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/customers");

        g.MapGet("", async (ListCustomersQueryHandler h, string? search, CancellationToken ct) =>
        {
            var customers = await h.HandleAsync(search, ct);
            return Results.Ok(customers.Select(c => new CustomerListDto(
                c.Id, c.FirstName, c.LastName, $"{c.FirstName} {c.LastName}".Trim(),
                c.Phone, c.Email, c.City)));
        });

        g.MapGet("/{id}", async (string id, GetCustomerByIdQueryHandler h, CancellationToken ct) =>
        {
            var c = await h.HandleAsync(id, includeComponents: true, ct);
            return c is null ? Results.NotFound() : Results.Ok(c);
        });

        g.MapPost("", async (CreateCustomerRequest req, CreateCustomerCommandHandler h, CancellationToken ct) =>
        {
            var result = await h.HandleAsync(req, ct);
            return Results.Created($"/api/customers/{result.Id}", result);
        });

        g.MapPut("/{id}", async (string id, UpdateCustomerRequest req, UpdateCustomerCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(req with { Id = id }, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        g.MapDelete("/{id}", async (string id, DeleteCustomerCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(new DeleteCustomerRequest(id), ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });
    }
}
