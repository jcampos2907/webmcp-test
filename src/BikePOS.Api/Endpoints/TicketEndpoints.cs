using BikePOS.Application.Commands;
using BikePOS.Application.Queries;
using BikePOS.Data;
using BikePOS.Models;
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;
using DomainPaymentMethod = BikePOS.Domain.Aggregates.ServiceTicket.PaymentMethod;

namespace BikePOS.Api.Endpoints;

public static class TicketEndpoints
{
    public record ChargeRequestDto(decimal Amount, string PaymentMethod, string? CashierName, string? TerminalId = null);
    public record RefundRequestDto(decimal Amount, string PaymentMethod, string? CashierName);
    public record ChangeStatusDto(string Status, string? ChangedBy);
    public record UpdateTicketDto(string? MechanicId, string? BaseServiceId, decimal BaseServicePrice, string? Description, decimal DiscountPercent, string? UpdatedBy);
    public record AddProductDto(string ProductId, int Quantity);
    public record UpdateProductQtyDto(int Quantity);

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
        {
            var tickets = await h.HandleAsync(q, ct);
            return Results.Ok(tickets.Select(t => new TicketListItemDto(
                t.Id, t.TicketDisplay, t.TicketNumber, t.Status,
                t.Component?.Name, t.Component?.ComponentType,
                t.Customer != null ? $"{t.Customer.FirstName} {t.Customer.LastName}".Trim() : null,
                t.Mechanic?.Name, t.Price, t.CreatedAt)));
        });

        g.MapPost("", async (CreateTicketRequest req, CreateTicketCommandHandler h, CancellationToken ct) =>
        {
            var r = await h.HandleAsync(req, ct);
            return Results.Created($"/api/tickets/{r.TicketId}", r);
        });

        g.MapPost("/{id}/cancel", async (string id, CancelTicketCommandHandler h, CancellationToken ct) =>
        {
            var ok = await h.HandleAsync(new CancelTicketRequest(id, null, null), ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        g.MapPost("/{id}/status", async (string id, ChangeStatusDto body, IDbContextFactory<BikePosContext> dbFactory, TicketEventService events, CancellationToken ct) =>
        {
            if (!Enum.TryParse<TicketStatus>(body.Status, true, out var newStatus))
                return Results.BadRequest(new { error = "Invalid status" });

            using var db = dbFactory.CreateDbContext();
            var ticket = await db.ServiceTicket.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (ticket is null) return Results.NotFound();
            if (ticket.Status == newStatus) return Results.NoContent();

            var oldStatus = ticket.Status;
            ticket.Status = newStatus;
            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.UpdatedBy = body.ChangedBy;
            await db.SaveChangesAsync(ct);

            await events.RecordStatusChange(id, oldStatus, newStatus, body.ChangedBy, ticket.StoreId);
            return Results.NoContent();
        });

        g.MapPost("/{id}/charges", async (string id, ChargeRequestDto body, ProcessChargeCommandHandler h, BikePOS.Services.TenantContext tenant, CancellationToken ct) =>
        {
            if (!Enum.TryParse<DomainPaymentMethod>(body.PaymentMethod, true, out var method))
                return Results.BadRequest(new { error = "Invalid payment method" });
            var result = await h.HandleAsync(new ProcessChargeRequest(id, body.Amount, method, body.CashierName, tenant.StoreId, body.TerminalId), ct);
            return result.ErrorMessage is null ? Results.Ok(result) : Results.BadRequest(result);
        });

        g.MapPost("/{id}/refunds", async (string id, RefundRequestDto body, ProcessRefundCommandHandler h, CancellationToken ct) =>
        {
            if (!Enum.TryParse<DomainPaymentMethod>(body.PaymentMethod, true, out var method))
                return Results.BadRequest(new { error = "Invalid payment method" });
            var result = await h.HandleAsync(new ProcessRefundRequest(id, body.Amount, method, body.CashierName, null), ct);
            return result.ErrorMessage is null ? Results.Ok(result) : Results.BadRequest(result);
        });

        g.MapPut("/{id}", async (string id, UpdateTicketDto body, IDbContextFactory<BikePosContext> dbFactory, TicketEventService events, CancellationToken ct) =>
        {
            using var db = dbFactory.CreateDbContext();
            var ticket = await db.ServiceTicket
                .Include(t => t.Mechanic)
                .Include(t => t.BaseService)
                .FirstOrDefaultAsync(t => t.Id == id, ct);
            if (ticket is null) return Results.NotFound();
            if (ticket.Status is TicketStatus.Charged or TicketStatus.Cancelled)
                return Results.BadRequest(new { error = "Ticket is read-only" });

            if (body.DiscountPercent < 0 || body.DiscountPercent > 100)
                return Results.BadRequest(new { error = "Discount must be 0–100" });

            var mechanicChanged = ticket.MechanicId != body.MechanicId;
            var descriptionChanged = ticket.Description != body.Description;
            var discountChanged = ticket.DiscountPercent != body.DiscountPercent;
            var oldDiscount = ticket.DiscountPercent;

            ticket.MechanicId = body.MechanicId;
            ticket.BaseServiceId = body.BaseServiceId;
            ticket.Price = body.BaseServicePrice;
            ticket.Description = body.Description;
            ticket.DiscountPercent = body.DiscountPercent;
            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.UpdatedBy = body.UpdatedBy;
            await db.SaveChangesAsync(ct);

            if (mechanicChanged)
            {
                var mechanicName = body.MechanicId is null ? null
                    : (await db.Mechanic.Where(m => m.Id == body.MechanicId).Select(m => m.Name).FirstOrDefaultAsync(ct));
                await events.RecordMechanicAssigned(id, mechanicName, body.UpdatedBy, ticket.StoreId);
            }
            if (descriptionChanged) await events.RecordNoteUpdated(id, body.UpdatedBy, ticket.StoreId);
            if (discountChanged) await events.RecordDiscountChanged(id, oldDiscount, body.DiscountPercent, body.UpdatedBy, ticket.StoreId);

            return Results.NoContent();
        });

        g.MapPost("/{id}/products", async (string id, AddProductDto body, IDbContextFactory<BikePosContext> dbFactory, TicketEventService events, CancellationToken ct) =>
        {
            if (body.Quantity <= 0) return Results.BadRequest(new { error = "Quantity must be positive" });
            using var db = dbFactory.CreateDbContext();
            var ticket = await db.ServiceTicket
                .Include(t => t.TicketProducts)
                .FirstOrDefaultAsync(t => t.Id == id, ct);
            if (ticket is null) return Results.NotFound();
            if (ticket.Status is TicketStatus.Charged or TicketStatus.Cancelled)
                return Results.BadRequest(new { error = "Ticket is read-only" });

            var product = await db.Product.FirstOrDefaultAsync(p => p.Id == body.ProductId, ct);
            if (product is null) return Results.BadRequest(new { error = "Product not found" });
            if (product.QuantityInStock < body.Quantity)
                return Results.BadRequest(new { error = "Not enough stock" });

            var existing = ticket.TicketProducts.FirstOrDefault(tp => tp.ProductId == body.ProductId);
            if (existing != null) existing.Quantity += body.Quantity;
            else db.TicketProduct.Add(new TicketProduct
            {
                ServiceTicketId = id, ProductId = body.ProductId,
                Quantity = body.Quantity, UnitPrice = product.Price,
            });
            product.QuantityInStock -= body.Quantity;
            ticket.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await events.RecordProductAdded(id, product.Name, body.Quantity, null, ticket.StoreId);
            return Results.NoContent();
        });

        g.MapDelete("/{id}/products/{productId}", async (string id, string productId, IDbContextFactory<BikePosContext> dbFactory, TicketEventService events, CancellationToken ct) =>
        {
            using var db = dbFactory.CreateDbContext();
            var ticket = await db.ServiceTicket
                .Include(t => t.TicketProducts).ThenInclude(tp => tp.Product)
                .FirstOrDefaultAsync(t => t.Id == id, ct);
            if (ticket is null) return Results.NotFound();
            if (ticket.Status is TicketStatus.Charged or TicketStatus.Cancelled)
                return Results.BadRequest(new { error = "Ticket is read-only" });

            var line = ticket.TicketProducts.FirstOrDefault(tp => tp.ProductId == productId);
            if (line is null) return Results.NotFound();
            var productName = line.Product?.Name ?? "";
            var qty = line.Quantity;

            var product = await db.Product.FirstOrDefaultAsync(p => p.Id == productId, ct);
            if (product != null) product.QuantityInStock += qty;
            db.TicketProduct.Remove(line);
            ticket.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await events.RecordProductRemoved(id, productName, qty, null, ticket.StoreId);
            return Results.NoContent();
        });
    }
}
