using BikePOS.Application.Commands;
using BikePOS.Application.Queries;
using BikePOS.Models;

namespace BikePOS.Api.Endpoints;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/tickets");

        g.MapGet("", async (ListTicketsQueryHandler h, string? status, CancellationToken ct) =>
        {
            TicketStatus? filter = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, true, out var s))
                filter = s;
            var tickets = await h.HandleAsync(filter, ct);
            return Results.Ok(tickets.Select(t => new TicketListItemDto(
                t.Id, t.TicketDisplay, t.TicketNumber, t.Status,
                t.Component?.Name, t.Component?.ComponentType,
                t.Customer != null ? $"{t.Customer.FirstName} {t.Customer.LastName}".Trim() : null,
                t.Mechanic?.Name, t.Price, t.CreatedAt)));
        });

        g.MapGet("/{id}", async (string id, GetTicketDetailsQueryHandler h, CancellationToken ct) =>
        {
            var dto = await h.HandleAsync(id, ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        g.MapGet("/search", async (string q, SearchTicketsQueryHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(q, ct)));

        g.MapPost("/{id}/cancel", async (string id, CancelTicketCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(new CancelTicketRequest(id, null, null), ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });
    }
}
