using BikePOS.Models;

namespace BikePOS.Services;

/// <summary>
/// Resolves the correct IPaymentTerminalProvider for a given terminal's provider type.
/// </summary>
public class PaymentTerminalService
{
    private readonly Dictionary<TerminalProvider, IPaymentTerminalProvider> _providers;

    public PaymentTerminalService(IEnumerable<IPaymentTerminalProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderType);
    }

    public IPaymentTerminalProvider GetProvider(TerminalProvider providerType)
    {
        if (_providers.TryGetValue(providerType, out var provider))
            return provider;
        throw new NotSupportedException($"No provider registered for terminal type: {providerType}");
    }

    public IPaymentTerminalProvider GetProvider(PaymentTerminal terminal)
        => GetProvider(terminal.Provider);

    public IEnumerable<TerminalProvider> SupportedProviders => _providers.Keys;
}
