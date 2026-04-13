using BikePOS.Data;
using BikePOS.Infrastructure.Printing;
using BikePOS.Interfaces.Services;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Commands;

public record PrintServiceTagRequest(
    string TicketId,
    string? PrinterId = null);

public record PrintServiceTagResult(bool Success, string? ErrorMessage = null);

public class PrintServiceTagCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly ReceiptPrinterService _printerService;

    public PrintServiceTagCommandHandler(
        IDbContextFactory<BikePosContext> dbFactory,
        ReceiptPrinterService printerService)
    {
        _dbFactory = dbFactory;
        _printerService = printerService;
    }

    public async Task<PrintServiceTagResult> HandleAsync(PrintServiceTagRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();

        var ticket = await db.ServiceTicket
            .Include(t => t.Component)
            .Include(t => t.Customer)
            .Include(t => t.Mechanic)
            .Include(t => t.BaseService)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);

        if (ticket is null)
            return new PrintServiceTagResult(false, "Ticket not found.");

        var store = await db.Store
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.Id == ticket.StoreId, ct);

        ReceiptPrinter? printer;
        if (request.PrinterId is not null)
        {
            printer = await db.ReceiptPrinter.FindAsync([request.PrinterId], ct);
        }
        else
        {
            printer = await db.ReceiptPrinter
                .Where(p => p.IsActive && p.StoreId == ticket.StoreId)
                .FirstOrDefaultAsync(ct);
        }

        if (printer is null)
            return new PrintServiceTagResult(false, "No printer available.");

        var tag = new ServiceTagContent(
            StoreName: store?.Company?.Name ?? store?.Name ?? "BikePOS",
            TicketDisplay: ticket.TicketDisplay,
            CustomerName: ticket.Customer is not null
                ? $"{ticket.Customer.FirstName} {ticket.Customer.LastName}"
                : "—",
            CustomerPhone: ticket.Customer?.Phone ?? "—",
            ComponentName: ticket.Component?.Name ?? "—",
            ComponentType: ticket.Component?.ComponentType ?? "—",
            ServiceName: ticket.BaseService?.Name ?? "—",
            MechanicName: ticket.Mechanic?.Name,
            Notes: ticket.Description,
            Date: ticket.CreatedAt
        );

        var provider = _printerService.GetProvider(printer);
        var printed = await provider.PrintServiceTagAsync(printer, tag);

        return printed
            ? new PrintServiceTagResult(true)
            : new PrintServiceTagResult(false, "Printer communication failed.");
    }
}
