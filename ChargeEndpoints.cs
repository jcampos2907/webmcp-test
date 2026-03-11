using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using BikePOS.Data;
using BikePOS.Models;

public static class ChargeEndpoints
{
    public static void MapChargeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Charge").WithTags(nameof(Charge));

        group.MapGet("/", async (BikePosContext db) =>
        {
            return await db.Charge.ToListAsync();
        })
        .WithName("GetAllCharges");

        group.MapGet("/recent", async (int? limit, BikePosContext db) =>
        {
            var take = limit ?? 10;
            return await db.Charge
                .Include(c => c.ServiceTicket)
                    .ThenInclude(t => t.Bike)
                .OrderByDescending(c => c.ChargedAt)
                .Take(take)
                .ToListAsync();
        })
        .WithName("GetRecentCharges");

        group.MapGet("/{id}", async Task<Results<Ok<Charge>, NotFound>> (int id, BikePosContext db) =>
        {
            return await db.Charge.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Charge model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetChargeById");

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int id, Charge charge, BikePosContext db) =>
        {
            var affected = await db.Charge
                .Where(model => model.Id == id)
                .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.ServiceTicketId, charge.ServiceTicketId)
                .SetProperty(m => m.ServiceTicket, charge.ServiceTicket)
                .SetProperty(m => m.Amount, charge.Amount)
                .SetProperty(m => m.ChargedAt, charge.ChargedAt)
                .SetProperty(m => m.CashierName, charge.CashierName)
                .SetProperty(m => m.PaymentMethod, charge.PaymentMethod)
                .SetProperty(m => m.ExternalTransactionId, charge.ExternalTransactionId)
                .SetProperty(m => m.Notes, charge.Notes)
        );

            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateCharge");

        group.MapPost("/", async (Charge charge, BikePosContext db) =>
        {
            db.Charge.Add(charge);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Charge/{charge.Id}", charge);
        })
        .WithName("CreateCharge");

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (int id, BikePosContext db) =>
        {
            var affected = await db.Charge
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();

            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteCharge");
    }
}
