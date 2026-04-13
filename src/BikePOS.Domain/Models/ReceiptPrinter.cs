using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public enum PrinterProvider
{
    Manual,
    EscPos
}

public class ReceiptPrinter
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(36)]
    public string StoreId { get; set; } = null!;
    public Store Store { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = "";

    public int Port { get; set; } = 9100;

    [Required]
    public PrinterProvider Provider { get; set; } = PrinterProvider.EscPos;

    public int PaperWidth { get; set; } = 48;

    public bool IsActive { get; set; } = true;

    public DateTime? LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
