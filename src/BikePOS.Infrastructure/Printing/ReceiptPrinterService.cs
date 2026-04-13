using BikePOS.Interfaces.Services;
using BikePOS.Models;

namespace BikePOS.Infrastructure.Printing;

public class ReceiptPrinterService
{
    private readonly Dictionary<PrinterProvider, IReceiptPrinterProvider> _providers;

    public ReceiptPrinterService(IEnumerable<IReceiptPrinterProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderType);
    }

    public IReceiptPrinterProvider GetProvider(PrinterProvider providerType)
    {
        if (_providers.TryGetValue(providerType, out var provider))
            return provider;
        throw new NotSupportedException($"No provider registered for printer type: {providerType}");
    }

    public IReceiptPrinterProvider GetProvider(ReceiptPrinter printer)
        => GetProvider(printer.Provider);
}
