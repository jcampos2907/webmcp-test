using BikePOS.Models;

namespace BikePOS.Services;

/// <summary>
/// Fallback provider for cash/transfer payments that don't use a physical terminal.
/// Payments are immediately marked as completed.
/// </summary>
public class ManualPaymentProvider : IPaymentTerminalProvider
{
    public TerminalProvider ProviderType => TerminalProvider.Manual;

    public Task<TerminalDevice[]> DiscoverDevicesAsync(string ipAddress, int port)
        => Task.FromResult(Array.Empty<TerminalDevice>());

    public Task<PaymentSession> CreatePaymentAsync(PaymentTerminal terminal, PaymentRequest request)
    {
        var session = new PaymentSession
        {
            TerminalId = terminal.Id,
            Status = PaymentSessionStatus.Completed,
            Amount = request.Amount,
            ExternalRef = $"manual-{Guid.NewGuid():N}",
            CompletedAt = DateTime.UtcNow
        };
        return Task.FromResult(session);
    }

    public Task<PaymentSessionStatus> GetStatusAsync(PaymentTerminal terminal, string externalRef)
        => Task.FromResult(PaymentSessionStatus.Completed);

    public Task<bool> CancelAsync(PaymentTerminal terminal, string externalRef)
        => Task.FromResult(true);

    public Task<bool> PingAsync(PaymentTerminal terminal)
        => Task.FromResult(true);
}
