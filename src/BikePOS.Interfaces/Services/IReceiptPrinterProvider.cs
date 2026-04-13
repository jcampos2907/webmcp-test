using BikePOS.Models;

namespace BikePOS.Interfaces.Services;

public record ReceiptLine(string Left, string Right = "");

public record ReceiptContent(
    string StoreName,
    string? StoreAddress,
    string? StorePhone,
    string? StoreTaxId,
    string TicketDisplay,
    string CustomerName,
    string ComponentName,
    string ServiceName,
    List<ReceiptLine> Items,
    decimal Subtotal,
    decimal DiscountPercent,
    decimal Total,
    string PaymentMethod,
    decimal AmountPaid,
    string? CashierName,
    DateTime Date,
    string CurrencySymbol
);

public record ServiceTagContent(
    string StoreName,
    string TicketDisplay,
    string CustomerName,
    string CustomerPhone,
    string ComponentName,
    string ComponentType,
    string ServiceName,
    string? MechanicName,
    string? Notes,
    DateTime Date
);

public interface IReceiptPrinterProvider
{
    PrinterProvider ProviderType { get; }

    Task<bool> PrintReceiptAsync(ReceiptPrinter printer, ReceiptContent receipt);

    Task<bool> PrintServiceTagAsync(ReceiptPrinter printer, ServiceTagContent tag);

    Task<bool> OpenCashDrawerAsync(ReceiptPrinter printer);

    Task<bool> PingAsync(ReceiptPrinter printer);
}
