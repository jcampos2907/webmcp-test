using BikePOS.Services;
using BikePOS.Data;
using BikePOS.Infrastructure.Printing;
using BikePOS.Interfaces.Services;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Commands;

public record PrintReceiptRequest(string ChargeId, string? PrinterId = null);

public record PrintReceiptResult(bool Success, string? ErrorMessage = null);

public class PrintReceiptCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly ReceiptPrinterService _printerService;
    private readonly ReportService _reportService;

    public PrintReceiptCommandHandler(
        IDbContextFactory<BikePosContext> dbFactory,
        ReceiptPrinterService printerService,
        ReportService reportService)
    {
        _dbFactory = dbFactory;
        _printerService = printerService;
        _reportService = reportService;
    }

    public async Task<PrintReceiptResult> HandleAsync(PrintReceiptRequest request, CancellationToken ct = default)
    {
        var receiptData = await _reportService.GetReceiptDataAsync(request.ChargeId);
        if (receiptData is null)
            return new PrintReceiptResult(false, "Charge not found.");

        using var db = _dbFactory.CreateDbContext();

        ReceiptPrinter? printer;
        if (request.PrinterId is not null)
        {
            printer = await db.ReceiptPrinter.FindAsync([request.PrinterId], ct);
        }
        else
        {
            var charge = await db.Charge.FindAsync([request.ChargeId], ct);
            printer = await db.ReceiptPrinter
                .Where(p => p.IsActive && p.StoreId == charge!.StoreId)
                .FirstOrDefaultAsync(ct);
        }

        if (printer is null)
            return new PrintReceiptResult(false, "No printer available.");

        var content = MapToReceiptContent(receiptData);
        var provider = _printerService.GetProvider(printer);
        var printed = await provider.PrintReceiptAsync(printer, content);

        return printed
            ? new PrintReceiptResult(true)
            : new PrintReceiptResult(false, "Printer communication failed.");
    }

    private static ReceiptContent MapToReceiptContent(ReceiptData data)
    {
        var items = new List<ReceiptLine>();

        if (data.ServiceName is not null)
            items.Add(new ReceiptLine(data.ServiceName, ""));

        foreach (var p in data.Products)
            items.Add(new ReceiptLine(
                $"{p.Name} x{p.Quantity}",
                p.LineTotal.ToString("N2")));

        return new ReceiptContent(
            StoreName: data.StoreName,
            StoreAddress: data.StoreAddress,
            StorePhone: data.StorePhone,
            StoreTaxId: data.CompanyTaxId,
            TicketDisplay: data.TicketDisplay,
            CustomerName: data.CustomerName ?? "—",
            ComponentName: data.ComponentName ?? "—",
            ServiceName: data.ServiceName ?? "—",
            Items: items,
            Subtotal: data.Subtotal,
            DiscountPercent: data.DiscountPercent,
            Total: data.Total,
            PaymentMethod: data.PaymentMethod,
            AmountPaid: data.AmountPaid,
            CashierName: data.CashierName,
            Date: data.ChargeDate,
            CurrencySymbol: "₡"
        );
    }
}
