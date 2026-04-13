using BikePOS.Interfaces.Services;
using BikePOS.Models;
using Microsoft.Extensions.Logging;

namespace BikePOS.Infrastructure.Printing;

public class SimulatedReceiptProvider : IReceiptPrinterProvider
{
    private readonly ILogger<SimulatedReceiptProvider> _logger;

    public PrinterProvider ProviderType => PrinterProvider.EscPos;

    public SimulatedReceiptProvider(ILogger<SimulatedReceiptProvider> logger)
    {
        _logger = logger;
    }

    public Task<bool> PrintReceiptAsync(ReceiptPrinter printer, ReceiptContent receipt)
    {
        _logger.LogInformation(
            "[SIMULATED PRINT] {Printer} — {Ticket} | {Customer} | {Total:C} ({Method})",
            printer.Name, receipt.TicketDisplay, receipt.CustomerName, receipt.Total, receipt.PaymentMethod);
        return Task.FromResult(true);
    }

    public Task<bool> PrintServiceTagAsync(ReceiptPrinter printer, ServiceTagContent tag)
    {
        _logger.LogInformation(
            "[SIMULATED TAG] {Printer} — {Ticket} | {Customer} | {Component} ({Service})",
            printer.Name, tag.TicketDisplay, tag.CustomerName, tag.ComponentName, tag.ServiceName);
        return Task.FromResult(true);
    }

    public Task<bool> OpenCashDrawerAsync(ReceiptPrinter printer)
    {
        _logger.LogInformation("[SIMULATED] Cash drawer opened on {Printer}", printer.Name);
        return Task.FromResult(true);
    }

    public Task<bool> PingAsync(ReceiptPrinter printer)
        => Task.FromResult(true);
}
