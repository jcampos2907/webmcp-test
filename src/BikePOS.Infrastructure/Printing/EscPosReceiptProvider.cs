using BikePOS.Interfaces.Services;
using BikePOS.Models;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;

namespace BikePOS.Infrastructure.Printing;

/// <summary>
/// ESC/POS thermal receipt printer provider over TCP (port 9100).
/// Compatible with Epson TM series, Star TSP, Bixolon, and most ESC/POS printers.
/// </summary>
public class EscPosReceiptProvider : IReceiptPrinterProvider
{
    private readonly ILogger<EscPosReceiptProvider> _logger;
    private const int ConnectTimeoutMs = 5000;

    public PrinterProvider ProviderType => PrinterProvider.EscPos;

    public EscPosReceiptProvider(ILogger<EscPosReceiptProvider> logger)
    {
        _logger = logger;
    }

    public async Task<bool> PrintReceiptAsync(ReceiptPrinter printer, ReceiptContent receipt)
    {
        try
        {
            var data = BuildReceipt(receipt, printer.PaperWidth);
            await SendToPrinter(printer.IpAddress, printer.Port, data);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Print failed on {Name} ({Ip}:{Port})", printer.Name, printer.IpAddress, printer.Port);
            return false;
        }
    }

    public async Task<bool> PrintServiceTagAsync(ReceiptPrinter printer, ServiceTagContent tag)
    {
        try
        {
            var data = BuildServiceTag(tag, printer.PaperWidth);
            await SendToPrinter(printer.IpAddress, printer.Port, data);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service tag print failed on {Name} ({Ip}:{Port})", printer.Name, printer.IpAddress, printer.Port);
            return false;
        }
    }

    public async Task<bool> OpenCashDrawerAsync(ReceiptPrinter printer)
    {
        try
        {
            await SendToPrinter(printer.IpAddress, printer.Port, EscPos.OpenDrawer);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cash drawer open failed on {Name}", printer.Name);
            return false;
        }
    }

    public async Task<bool> PingAsync(ReceiptPrinter printer)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(ConnectTimeoutMs);
            await client.ConnectAsync(printer.IpAddress, printer.Port, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Receipt builder ────────────────────────────────────────────

    private static byte[] BuildReceipt(ReceiptContent r, int width)
    {
        var buf = new List<byte>();

        buf.AddRange(EscPos.Init);

        // Store header — centered, bold, double-height
        buf.AddRange(EscPos.AlignCenter);
        buf.AddRange(EscPos.BoldOn);
        buf.AddRange(EscPos.DoubleHeight);
        buf.AddRange(Encode(r.StoreName));
        buf.AddRange(EscPos.NormalSize);
        buf.AddRange(EscPos.BoldOff);

        if (!string.IsNullOrEmpty(r.StoreAddress))
            buf.AddRange(Encode(r.StoreAddress));
        if (!string.IsNullOrEmpty(r.StorePhone))
            buf.AddRange(Encode(r.StorePhone));
        if (!string.IsNullOrEmpty(r.StoreTaxId))
            buf.AddRange(Encode($"RUC: {r.StoreTaxId}"));

        buf.AddRange(EscPos.AlignLeft);
        buf.AddRange(Separator(width));

        // Ticket info
        buf.AddRange(EscPos.BoldOn);
        buf.AddRange(Encode(r.TicketDisplay));
        buf.AddRange(EscPos.BoldOff);
        buf.AddRange(Encode(r.Date.ToString("dd/MM/yyyy HH:mm")));
        buf.AddRange(Encode($"Cliente: {r.CustomerName}"));
        buf.AddRange(Encode($"Componente: {r.ComponentName}"));
        buf.AddRange(Encode($"Servicio: {r.ServiceName}"));

        buf.AddRange(Separator(width));

        // Line items
        buf.AddRange(TwoColumn("ITEM", "TOTAL", width));
        buf.AddRange(Separator(width, '-'));

        foreach (var item in r.Items)
        {
            buf.AddRange(TwoColumn(item.Left, item.Right, width));
        }

        buf.AddRange(Separator(width));

        // Totals
        buf.AddRange(TwoColumn("Subtotal", FormatCurrency(r.Subtotal, r.CurrencySymbol), width));
        if (r.DiscountPercent > 0)
            buf.AddRange(TwoColumn($"Descuento ({r.DiscountPercent}%)",
                $"-{FormatCurrency(r.Subtotal * r.DiscountPercent / 100, r.CurrencySymbol)}", width));

        buf.AddRange(EscPos.BoldOn);
        buf.AddRange(TwoColumn("TOTAL", FormatCurrency(r.Total, r.CurrencySymbol), width));
        buf.AddRange(EscPos.BoldOff);

        buf.AddRange(Separator(width));

        // Payment
        buf.AddRange(TwoColumn("Método", r.PaymentMethod, width));
        buf.AddRange(TwoColumn("Pagado", FormatCurrency(r.AmountPaid, r.CurrencySymbol), width));

        if (!string.IsNullOrEmpty(r.CashierName))
            buf.AddRange(TwoColumn("Cajero", r.CashierName, width));

        buf.AddRange(Separator(width));

        // Footer
        buf.AddRange(EscPos.AlignCenter);
        buf.AddRange(Encode("¡Gracias por su preferencia!"));
        buf.AddRange(EscPos.LineFeed);
        buf.AddRange(EscPos.LineFeed);

        // Cut paper
        buf.AddRange(EscPos.Cut);

        return buf.ToArray();
    }

    private static byte[] BuildServiceTag(ServiceTagContent t, int width)
    {
        var buf = new List<byte>();

        buf.AddRange(EscPos.Init);

        // Store header
        buf.AddRange(EscPos.AlignCenter);
        buf.AddRange(EscPos.BoldOn);
        buf.AddRange(EscPos.DoubleHeight);
        buf.AddRange(Encode(t.StoreName));
        buf.AddRange(EscPos.NormalSize);

        // Ticket number — large and prominent
        buf.AddRange(EscPos.DoubleHeight);
        buf.AddRange(Encode(t.TicketDisplay));
        buf.AddRange(EscPos.NormalSize);
        buf.AddRange(EscPos.BoldOff);

        buf.AddRange(Encode(t.Date.ToString("dd/MM/yyyy HH:mm")));
        buf.AddRange(EscPos.AlignLeft);
        buf.AddRange(Separator(width));

        buf.AddRange(TwoColumn("Cliente:", t.CustomerName, width));
        buf.AddRange(TwoColumn("Tel:", t.CustomerPhone, width));
        buf.AddRange(Separator(width, '-'));
        buf.AddRange(TwoColumn("Componente:", t.ComponentName, width));
        buf.AddRange(TwoColumn("Tipo:", t.ComponentType, width));
        buf.AddRange(TwoColumn("Servicio:", t.ServiceName, width));
        if (!string.IsNullOrEmpty(t.MechanicName))
            buf.AddRange(TwoColumn("Mecánico:", t.MechanicName, width));

        if (!string.IsNullOrEmpty(t.Notes))
        {
            buf.AddRange(Separator(width, '-'));
            buf.AddRange(Encode($"Notas: {t.Notes}"));
        }

        buf.AddRange(Separator(width));

        // Footer
        buf.AddRange(EscPos.AlignCenter);
        buf.AddRange(Encode("Adjuntar a componente"));
        buf.AddRange(EscPos.LineFeed);
        buf.AddRange(EscPos.LineFeed);
        buf.AddRange(EscPos.Cut);

        return buf.ToArray();
    }

    // ── ESC/POS command constants ──────────────────────────────────

    private static class EscPos
    {
        public static readonly byte[] Init = [0x1B, 0x40]; // ESC @
        public static readonly byte[] BoldOn = [0x1B, 0x45, 0x01]; // ESC E 1
        public static readonly byte[] BoldOff = [0x1B, 0x45, 0x00]; // ESC E 0
        public static readonly byte[] AlignLeft = [0x1B, 0x61, 0x00]; // ESC a 0
        public static readonly byte[] AlignCenter = [0x1B, 0x61, 0x01]; // ESC a 1
        public static readonly byte[] DoubleHeight = [0x1D, 0x21, 0x01]; // GS ! 1
        public static readonly byte[] NormalSize = [0x1D, 0x21, 0x00]; // GS ! 0
        public static readonly byte[] LineFeed = [0x0A]; // LF
        public static readonly byte[] Cut = [0x1D, 0x56, 0x00]; // GS V 0 (full cut)
        public static readonly byte[] OpenDrawer = [0x1B, 0x70, 0x00, 0x19, 0xFA]; // ESC p 0 25 250
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static byte[] Encode(string text)
    {
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.GetEncoding("ibm850").GetBytes(text));
        bytes.AddRange(EscPos.LineFeed);
        return bytes.ToArray();
    }

    private static byte[] TwoColumn(string left, string right, int width)
    {
        var gap = width - left.Length - right.Length;
        if (gap < 1)
        {
            var maxLeft = width - right.Length - 1;
            left = left.Length > maxLeft ? left[..maxLeft] : left;
            gap = width - left.Length - right.Length;
        }
        return Encode($"{left}{new string(' ', gap)}{right}");
    }

    private static byte[] Separator(int width, char ch = '=')
        => Encode(new string(ch, width));

    private static string FormatCurrency(decimal amount, string symbol)
        => $"{symbol}{amount:N2}";

    private static async Task SendToPrinter(string ip, int port, byte[] data)
    {
        using var client = new TcpClient();
        using var cts = new CancellationTokenSource(ConnectTimeoutMs);
        await client.ConnectAsync(ip, port, cts.Token);
        var stream = client.GetStream();
        await stream.WriteAsync(data);
        await stream.FlushAsync();
    }
}
