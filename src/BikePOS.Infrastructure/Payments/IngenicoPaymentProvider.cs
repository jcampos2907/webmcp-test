using BikePOS.Interfaces.Services;
using BikePOS.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace BikePOS.Infrastructure.Payments;

/// <summary>
/// Ingenico TETRA/Axium terminal provider using the Telium ECR protocol over TCP.
/// Communicates with the terminal via length-prefixed XML messages.
///
/// Protocol flow:
///   1. ECR opens TCP connection to terminal IP:port
///   2. ECR sends ServiceRequest (payment, cancel, status, etc.)
///   3. Terminal processes — customer taps/inserts card, enters PIN
///   4. Terminal sends ServiceResponse with approval/decline
///   5. Connection closes or stays alive for status polling
/// </summary>
public class IngenicoPaymentProvider : IPaymentTerminalProvider
{
    private readonly ILogger<IngenicoPaymentProvider> _logger;
    private readonly ConcurrentDictionary<string, TransactionState> _activeTransactions = new();

    private const int ConnectTimeoutMs = 5000;
    private const int ReadTimeoutMs = 30000;

    public TerminalProvider ProviderType => TerminalProvider.Ingenico;

    public IngenicoPaymentProvider(ILogger<IngenicoPaymentProvider> logger)
    {
        _logger = logger;
    }

    public async Task<TerminalDevice[]> DiscoverDevicesAsync(string ipAddress, int port)
    {
        try
        {
            using var client = await ConnectAsync(ipAddress, port);
            var statusXml = BuildMessage("Status", new Dictionary<string, string>
            {
                ["MessageType"] = "StatusRequest"
            });

            await SendMessageAsync(client, statusXml);
            var response = await ReadMessageAsync(client);

            var doc = XDocument.Parse(response);
            var terminalId = doc.Root?.Element("TerminalId")?.Value ?? "unknown";
            var model = doc.Root?.Element("Model")?.Value ?? "Ingenico Terminal";

            return [new TerminalDevice(terminalId, $"{model} @ {ipAddress}:{port}", true)];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Discovery failed for {Ip}:{Port}", ipAddress, port);
            return [];
        }
    }

    public async Task<PaymentSession> CreatePaymentAsync(PaymentTerminal terminal, PaymentRequest request)
    {
        var transactionId = $"txn-{Guid.NewGuid():N}";

        try
        {
            using var client = await ConnectAsync(terminal.IpAddress, terminal.Port);

            var amountCents = (long)(request.Amount * 100);
            var paymentXml = BuildMessage("Payment", new Dictionary<string, string>
            {
                ["MessageType"] = "PaymentRequest",
                ["TransactionId"] = transactionId,
                ["Amount"] = amountCents.ToString(),
                ["Currency"] = request.Currency ?? "CRC",
                ["Reference"] = request.Reference ?? "",
                ["TransactionType"] = "Sale"
            });

            await SendMessageAsync(client, paymentXml);
            var response = await ReadMessageAsync(client);

            var doc = XDocument.Parse(response);
            var status = doc.Root?.Element("Status")?.Value;

            if (status == "Accepted")
            {
                _activeTransactions[transactionId] = new TransactionState
                {
                    Status = PaymentSessionStatus.Processing,
                    TerminalIp = terminal.IpAddress,
                    TerminalPort = terminal.Port,
                    CreatedAt = DateTime.UtcNow
                };

                return new PaymentSession
                {
                    TerminalId = terminal.Id,
                    Status = PaymentSessionStatus.Processing,
                    Amount = request.Amount,
                    ExternalRef = transactionId
                };
            }

            var errorMsg = doc.Root?.Element("ErrorMessage")?.Value ?? "Terminal rejected the payment request";
            _logger.LogWarning("Payment request rejected: {Error}", errorMsg);

            return new PaymentSession
            {
                TerminalId = terminal.Id,
                Status = PaymentSessionStatus.Failed,
                Amount = request.Amount,
                ExternalRef = transactionId,
                ErrorMessage = errorMsg
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment on terminal {Name} ({Ip}:{Port})",
                terminal.Name, terminal.IpAddress, terminal.Port);

            return new PaymentSession
            {
                TerminalId = terminal.Id,
                Status = PaymentSessionStatus.Failed,
                Amount = request.Amount,
                ExternalRef = transactionId,
                ErrorMessage = $"Connection error: {ex.Message}"
            };
        }
    }

    public async Task<PaymentSessionStatus> GetStatusAsync(PaymentTerminal terminal, string externalRef)
    {
        if (_activeTransactions.TryGetValue(externalRef, out var state) &&
            state.Status is not PaymentSessionStatus.Processing)
        {
            return state.Status;
        }

        try
        {
            using var client = await ConnectAsync(terminal.IpAddress, terminal.Port);

            var statusXml = BuildMessage("TransactionStatus", new Dictionary<string, string>
            {
                ["MessageType"] = "StatusRequest",
                ["TransactionId"] = externalRef
            });

            await SendMessageAsync(client, statusXml);
            var response = await ReadMessageAsync(client);

            var doc = XDocument.Parse(response);
            var statusStr = doc.Root?.Element("TransactionStatus")?.Value;

            var newStatus = statusStr switch
            {
                "Approved" or "Completed" => PaymentSessionStatus.Completed,
                "Declined" or "Error" => PaymentSessionStatus.Failed,
                "Cancelled" or "Aborted" => PaymentSessionStatus.Cancelled,
                _ => PaymentSessionStatus.Processing
            };

            if (_activeTransactions.TryGetValue(externalRef, out var txn))
                txn.Status = newStatus;

            return newStatus;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Status check failed for transaction {Ref}", externalRef);
            return state?.Status ?? PaymentSessionStatus.Processing;
        }
    }

    public async Task<bool> CancelAsync(PaymentTerminal terminal, string externalRef)
    {
        try
        {
            using var client = await ConnectAsync(terminal.IpAddress, terminal.Port);

            var cancelXml = BuildMessage("Cancel", new Dictionary<string, string>
            {
                ["MessageType"] = "CancelRequest",
                ["TransactionId"] = externalRef
            });

            await SendMessageAsync(client, cancelXml);
            var response = await ReadMessageAsync(client);

            var doc = XDocument.Parse(response);
            var result = doc.Root?.Element("Status")?.Value;

            if (result is "Cancelled" or "Accepted")
            {
                if (_activeTransactions.TryGetValue(externalRef, out var txn))
                    txn.Status = PaymentSessionStatus.Cancelled;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cancel failed for transaction {Ref}", externalRef);
            if (_activeTransactions.TryGetValue(externalRef, out var txn))
                txn.Status = PaymentSessionStatus.Cancelled;
            return true;
        }
    }

    public async Task<bool> PingAsync(PaymentTerminal terminal)
    {
        try
        {
            using var client = await ConnectAsync(terminal.IpAddress, terminal.Port);

            var pingXml = BuildMessage("Ping", new Dictionary<string, string>
            {
                ["MessageType"] = "PingRequest"
            });

            await SendMessageAsync(client, pingXml);
            var response = await ReadMessageAsync(client);

            return !string.IsNullOrEmpty(response);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ping failed for {Ip}:{Port}", terminal.IpAddress, terminal.Port);
            return false;
        }
    }

    // ── TCP transport ──────────────────────────────────────────────

    private static async Task<TcpClient> ConnectAsync(string ip, int port)
    {
        var client = new TcpClient();
        using var cts = new CancellationTokenSource(ConnectTimeoutMs);
        await client.ConnectAsync(ip, port, cts.Token);
        return client;
    }

    /// <summary>
    /// Sends a length-prefixed UTF-8 message: [4-byte big-endian length][payload]
    /// </summary>
    private static async Task SendMessageAsync(TcpClient client, string message)
    {
        var payload = Encoding.UTF8.GetBytes(message);
        var lengthPrefix = BitConverter.GetBytes(payload.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthPrefix);

        var stream = client.GetStream();
        await stream.WriteAsync(lengthPrefix);
        await stream.WriteAsync(payload);
        await stream.FlushAsync();
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 message: [4-byte big-endian length][payload]
    /// </summary>
    private static async Task<string> ReadMessageAsync(TcpClient client)
    {
        var stream = client.GetStream();
        stream.ReadTimeout = ReadTimeoutMs;

        var lengthBuffer = new byte[4];
        await ReadExactAsync(stream, lengthBuffer);
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBuffer);
        var length = BitConverter.ToInt32(lengthBuffer, 0);

        if (length <= 0 || length > 1024 * 64)
            throw new InvalidOperationException($"Invalid message length: {length}");

        var payload = new byte[length];
        await ReadExactAsync(stream, payload);
        return Encoding.UTF8.GetString(payload);
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset));
            if (read == 0) throw new IOException("Connection closed by terminal");
            offset += read;
        }
    }

    // ── Message building ───────────────────────────────────────────

    private static string BuildMessage(string rootElement, Dictionary<string, string> fields)
    {
        var root = new XElement(rootElement);
        foreach (var (key, value) in fields)
            root.Add(new XElement(key, value));
        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString(SaveOptions.DisableFormatting);
    }

    // ── Internal state ─────────────────────────────────────────────

    private class TransactionState
    {
        public PaymentSessionStatus Status { get; set; }
        public string TerminalIp { get; set; } = "";
        public int TerminalPort { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
