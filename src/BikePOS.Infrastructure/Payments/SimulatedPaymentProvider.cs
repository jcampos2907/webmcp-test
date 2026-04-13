using BikePOS.Interfaces.Services;
using BikePOS.Models;
using System.Collections.Concurrent;

namespace BikePOS.Infrastructure.Payments;

/// <summary>
/// Simulated provider that mimics terminal behavior for testing without hardware.
/// Maps to all terminal types when registered, so seed terminals work out of the box.
/// </summary>
public class SimulatedPaymentProvider : IPaymentTerminalProvider
{
    private readonly TerminalProvider _providerType;

    public SimulatedPaymentProvider(TerminalProvider providerType = TerminalProvider.Ingenico)
    {
        _providerType = providerType;
    }

    public TerminalProvider ProviderType => _providerType;

    private readonly ConcurrentDictionary<string, (PaymentSessionStatus Status, DateTime CreatedAt)> _sessions = new();

    public Task<TerminalDevice[]> DiscoverDevicesAsync(string ipAddress, int port)
        => Task.FromResult(new[] { new TerminalDevice("sim-001", $"Simulated @ {ipAddress}:{port}", true) });

    public Task<PaymentSession> CreatePaymentAsync(PaymentTerminal terminal, PaymentRequest request)
    {
        var externalRef = $"sim-{Guid.NewGuid():N}";
        _sessions[externalRef] = (PaymentSessionStatus.Processing, DateTime.UtcNow);

        var session = new PaymentSession
        {
            TerminalId = terminal.Id,
            Status = PaymentSessionStatus.Processing,
            Amount = request.Amount,
            ExternalRef = externalRef
        };
        return Task.FromResult(session);
    }

    public Task<PaymentSessionStatus> GetStatusAsync(PaymentTerminal terminal, string externalRef)
    {
        if (!_sessions.TryGetValue(externalRef, out var entry))
            return Task.FromResult(PaymentSessionStatus.Failed);

        if (DateTime.UtcNow - entry.CreatedAt > TimeSpan.FromSeconds(3))
        {
            _sessions[externalRef] = (PaymentSessionStatus.Completed, entry.CreatedAt);
            return Task.FromResult(PaymentSessionStatus.Completed);
        }

        return Task.FromResult(PaymentSessionStatus.Processing);
    }

    public Task<bool> CancelAsync(PaymentTerminal terminal, string externalRef)
    {
        if (_sessions.TryGetValue(externalRef, out _))
        {
            _sessions[externalRef] = (PaymentSessionStatus.Cancelled, DateTime.UtcNow);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> PingAsync(PaymentTerminal terminal)
        => Task.FromResult(true);
}
