using BikePOS.Models;

namespace BikePOS.Services;

public record PaymentRequest(decimal Amount, string Currency, string? Reference = null);

public record TerminalDevice(string DeviceId, string Name, bool IsOnline);

public interface IPaymentTerminalProvider
{
    TerminalProvider ProviderType { get; }

    Task<TerminalDevice[]> DiscoverDevicesAsync(string ipAddress, int port);

    Task<PaymentSession> CreatePaymentAsync(PaymentTerminal terminal, PaymentRequest request);

    Task<PaymentSessionStatus> GetStatusAsync(PaymentTerminal terminal, string externalRef);

    Task<bool> CancelAsync(PaymentTerminal terminal, string externalRef);

    Task<bool> PingAsync(PaymentTerminal terminal);
}
