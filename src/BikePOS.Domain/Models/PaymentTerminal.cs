using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public enum TerminalProvider
{
    Manual,
    Ingenico,
    Verifone,
    PAX,
    Nexgo
}

public class PaymentTerminal
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

    public int Port { get; set; } = 8080;

    [Required]
    public TerminalProvider Provider { get; set; } = TerminalProvider.Manual;

    public bool IsActive { get; set; } = true;

    public DateTime? LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
