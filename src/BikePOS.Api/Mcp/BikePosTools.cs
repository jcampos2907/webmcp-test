using System.ComponentModel;
using BikePOS.Application.Commands;
using BikePOS.Application.DTOs;
using BikePOS.Application.Queries;
using BikePOS.Data;
using BikePOS.Models;
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace BikePOS.Api.Mcp;

/// <summary>
/// Shop-facing tools exposed over MCP. Every tool runs inside the caller's request
/// scope so it inherits the auth cookie + TenantContext — store scoping and
/// permission checks are enforced by the same PermissionGuard/handlers the REST
/// endpoints use.
/// </summary>
[McpServerToolType]
public class BikePosTools
{
    // ────────────────── READ TOOLS ──────────────────

    public record TicketSummaryDto(string Id, string TicketDisplay, string Status, string? ComponentName,
        string? CustomerName, string? MechanicName, decimal Price, DateTime CreatedAt);

    [McpServerTool(Name = "list_tickets")]
    [Description("Lists service tickets at the active store. Optional status filter: Open, InProgress, Completed, Charged, Cancelled.")]
    public static async Task<IReadOnlyList<TicketSummaryDto>> ListTickets(
        ListTicketsQueryHandler handler,
        [Description("Optional status filter.")] string? status,
        CancellationToken ct)
    {
        TicketStatus? filter = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, true, out var s))
            filter = s;
        var tickets = await handler.HandleAsync(filter, ct);
        return tickets.Select(t => new TicketSummaryDto(
            t.Id, t.TicketDisplay, t.Status.ToString(),
            t.Component?.Name,
            t.Customer != null ? $"{t.Customer.FirstName} {t.Customer.LastName}".Trim() : null,
            t.Mechanic?.Name, t.Price, t.CreatedAt)).ToList();
    }

    [McpServerTool(Name = "get_ticket")]
    [Description("Returns full details for a single ticket including charges, products, and timeline.")]
    public static async Task<TicketDetailsDto?> GetTicket(
        GetTicketDetailsQueryHandler handler,
        [Description("Ticket id.")] string ticketId,
        CancellationToken ct) => await handler.HandleAsync(ticketId, ct);

    public record CustomerSummaryDto(string Id, string FullName, string? Phone, string? Email, string? City);

    [McpServerTool(Name = "list_customers")]
    [Description("Lists customers at the active store. No search filter — returns all.")]
    public static async Task<IReadOnlyList<CustomerSummaryDto>> ListCustomers(
        ListCustomersQueryHandler handler,
        CancellationToken ct)
    {
        var items = await handler.HandleAsync(null, ct);
        return items.Select(c => new CustomerSummaryDto(
            c.Id, $"{c.FirstName} {c.LastName}".Trim(), c.Phone, c.Email, c.City)).ToList();
    }

    [McpServerTool(Name = "search_customers")]
    [Description("Searches customers by name, phone, or email substring at the active store.")]
    public static async Task<IReadOnlyList<CustomerSummaryDto>> SearchCustomers(
        ListCustomersQueryHandler handler,
        [Description("Search substring — matched against name, phone, email.")] string query,
        CancellationToken ct)
    {
        var items = await handler.HandleAsync(query, ct);
        return items.Select(c => new CustomerSummaryDto(
            c.Id, $"{c.FirstName} {c.LastName}".Trim(), c.Phone, c.Email, c.City)).ToList();
    }

    public record ProductSummaryDto(string Id, string Name, string? Sku, decimal Price, int QuantityInStock, string? Category);

    [McpServerTool(Name = "list_products")]
    [Description("Lists products at the active store with stock and pricing.")]
    public static async Task<IReadOnlyList<ProductSummaryDto>> ListProducts(
        ListProductsQueryHandler handler,
        CancellationToken ct)
    {
        var items = await handler.HandleAsync(null, ct);
        return items.Select(p => new ProductSummaryDto(p.Id, p.Name, p.Sku, p.Price, p.QuantityInStock, p.Category)).ToList();
    }

    [McpServerTool(Name = "search_products")]
    [Description("Searches products by name, SKU, or category substring.")]
    public static async Task<IReadOnlyList<ProductSummaryDto>> SearchProducts(
        ListProductsQueryHandler handler,
        [Description("Search substring — matched against name, SKU, category.")] string query,
        CancellationToken ct)
    {
        var items = await handler.HandleAsync(query, ct);
        return items.Select(p => new ProductSummaryDto(p.Id, p.Name, p.Sku, p.Price, p.QuantityInStock, p.Category)).ToList();
    }

    public record ServiceDto(string Id, string Name, string? Description, decimal DefaultPrice, int? EstimatedMinutes);

    [McpServerTool(Name = "list_services")]
    [Description("Lists all services offered at the active store.")]
    public static async Task<IReadOnlyList<ServiceDto>> ListServices(
        IDbContextFactory<BikePosContext> factory,
        TenantContext tenant,
        CancellationToken ct)
    {
        using var db = factory.CreateDbContext();
        db.CurrentStoreId = tenant.StoreId;
        var items = await db.Service.OrderBy(s => s.Name).ToListAsync(ct);
        return items.Select(s => new ServiceDto(s.Id, s.Name, s.Description, s.DefaultPrice, s.EstimatedMinutes)).ToList();
    }

    public record DailySalesDto(DateOnly Date, int Transactions, decimal Revenue);

    [McpServerTool(Name = "get_daily_sales")]
    [Description("Returns daily sales aggregates between two dates. Dates are ISO yyyy-MM-dd.")]
    public static async Task<IReadOnlyList<DailySalesDto>> GetDailySales(
        DailySalesQueryHandler handler,
        [Description("Start date, ISO yyyy-MM-dd.")] string from,
        [Description("End date, ISO yyyy-MM-dd.")] string to,
        CancellationToken ct)
    {
        var fromDate = DateOnly.Parse(from);
        var toDate = DateOnly.Parse(to);
        var rows = await handler.HandleAsync(fromDate, toDate, ct);
        return rows.Select(r => new DailySalesDto(r.Date, r.Transactions, r.Revenue)).ToList();
    }

    // ────────────────── WRITE TOOLS ──────────────────
    // Reuse handlers where they exist (they carry their own PermissionGuard calls).
    // Inline writes call guard.Require directly, mirroring the REST endpoints.

    [McpServerTool(Name = "assign_mechanic")]
    [Description("Assigns a mechanic to a ticket. Caller needs 'tickets.manage'.")]
    public static async Task<string> AssignMechanic(
        PermissionGuard guard,
        IDbContextFactory<BikePosContext> factory,
        TicketEventService events,
        TenantContext tenant,
        [Description("Ticket id.")] string ticketId,
        [Description("Mechanic id, or null to unassign.")] string? mechanicId,
        CancellationToken ct)
    {
        guard.Require("tickets.manage");
        using var db = factory.CreateDbContext();
        var ticket = await db.ServiceTicket.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null) return $"Ticket {ticketId} not found.";
        if (ticket.Status is TicketStatus.Charged or TicketStatus.Cancelled)
            return $"Ticket {ticket.TicketDisplay} is {ticket.Status} and read-only.";

        ticket.MechanicId = mechanicId;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var name = mechanicId is null ? null
            : await db.Mechanic.Where(m => m.Id == mechanicId).Select(m => m.Name).FirstOrDefaultAsync(ct);
        await events.RecordMechanicAssigned(ticketId, name, tenant.DisplayName, ticket.StoreId);
        return name is null ? $"Unassigned mechanic from {ticket.TicketDisplay}."
                            : $"Assigned {name} to {ticket.TicketDisplay}.";
    }

    [McpServerTool(Name = "update_ticket_description")]
    [Description("Updates the free-text description on a ticket. Caller needs 'tickets.manage'.")]
    public static async Task<string> UpdateTicketDescription(
        PermissionGuard guard,
        IDbContextFactory<BikePosContext> factory,
        TicketEventService events,
        TenantContext tenant,
        [Description("Ticket id.")] string ticketId,
        [Description("New description text.")] string description,
        CancellationToken ct)
    {
        guard.Require("tickets.manage");
        using var db = factory.CreateDbContext();
        var ticket = await db.ServiceTicket.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null) return $"Ticket {ticketId} not found.";
        if (ticket.Status is TicketStatus.Charged or TicketStatus.Cancelled)
            return $"Ticket {ticket.TicketDisplay} is {ticket.Status} and read-only.";

        ticket.Description = description;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await events.RecordNoteUpdated(ticketId, tenant.DisplayName, ticket.StoreId);
        return $"Updated description on {ticket.TicketDisplay}.";
    }

    [McpServerTool(Name = "add_product_to_ticket")]
    [Description("Adds a product line to a ticket, decrementing stock. Caller needs 'tickets.manage'.")]
    public static async Task<string> AddProductToTicket(
        PermissionGuard guard,
        IDbContextFactory<BikePosContext> factory,
        TicketEventService events,
        [Description("Ticket id.")] string ticketId,
        [Description("Product id.")] string productId,
        [Description("Quantity to add (must be positive).")] int quantity,
        CancellationToken ct)
    {
        guard.Require("tickets.manage");
        if (quantity <= 0) return "Quantity must be positive.";

        using var db = factory.CreateDbContext();
        var ticket = await db.ServiceTicket
            .Include(t => t.TicketProducts)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null) return $"Ticket {ticketId} not found.";
        if (ticket.Status is TicketStatus.Charged or TicketStatus.Cancelled)
            return $"Ticket {ticket.TicketDisplay} is read-only.";

        var product = await db.Product.FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null) return $"Product {productId} not found.";
        if (product.QuantityInStock < quantity)
            return $"Not enough stock: {product.Name} has {product.QuantityInStock} available.";

        var existing = ticket.TicketProducts.FirstOrDefault(tp => tp.ProductId == productId);
        if (existing != null) existing.Quantity += quantity;
        else db.TicketProduct.Add(new TicketProduct
        {
            ServiceTicketId = ticketId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = product.Price,
        });
        product.QuantityInStock -= quantity;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await events.RecordProductAdded(ticketId, product.Name, quantity, null, ticket.StoreId);
        return $"Added {quantity}× {product.Name} to {ticket.TicketDisplay}.";
    }

    [McpServerTool(Name = "create_ticket")]
    [Description("Creates a new service ticket. Handler enforces store scoping and records domain events.")]
    public static async Task<CreateTicketResult> CreateTicket(
        CreateTicketCommandHandler handler,
        TenantContext tenant,
        [Description("Component id (bike, rim, other) the ticket is for.")] string componentId,
        [Description("Customer id, or null for walk-in.")] string? customerId,
        [Description("Mechanic id, or null to leave unassigned.")] string? mechanicId,
        [Description("Base service id, or null for custom.")] string? baseServiceId,
        [Description("Base service price.")] decimal baseServicePrice,
        [Description("Free-text description.")] string? description,
        CancellationToken ct)
    {
        var req = new CreateTicketRequest(
            ComponentId: componentId,
            CustomerId: customerId,
            MechanicId: mechanicId,
            BaseServiceId: baseServiceId,
            BaseServicePrice: baseServicePrice,
            Description: description,
            DiscountPercent: 0,
            StoreId: tenant.StoreId,
            CreatedBy: tenant.DisplayName,
            Products: new List<ProductLineRequest>());
        return await handler.HandleAsync(req, ct);
    }

    [McpServerTool(Name = "suggest_ticket_description")]
    [Description("Returns an AI-suggested ticket description given a bike/component name and a base service. Currently returns a templated suggestion; will route through an LLM later.")]
    public static Task<string> SuggestTicketDescription(
        [Description("Bike or component name, e.g. 'Trek FX 3 Disc'.")] string bike,
        [Description("Base service name, e.g. 'Full tune-up'.")] string service)
    {
        // Stub: deterministic template. Swap for an LLM call when the chat loop is live.
        var suggestion = $"{service} on {bike}: inspect drivetrain, true wheels, adjust derailleurs, " +
                         $"check brake pads and cable tension, lubricate chain, confirm tire pressure. " +
                         $"Flag any worn components for customer approval before replacing.";
        return Task.FromResult(suggestion);
    }

    [McpServerTool(Name = "delete_product")]
    [Description("Deletes a product by id. Enforcement lives in DeleteProductCommandHandler (requires 'products.manage').")]
    public static async Task<string> DeleteProduct(
        DeleteProductCommandHandler handler,
        [Description("Product id to delete.")] string productId,
        CancellationToken ct)
    {
        var ok = await handler.HandleAsync(new DeleteProductRequest(productId), ct);
        return ok ? $"Deleted product {productId}." : $"Product {productId} not found.";
    }
}
