using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public enum PaymentSessionStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public class PaymentSession
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(36)]
    public string ChargeId { get; set; } = null!;
    public Charge Charge { get; set; } = null!;

    [MaxLength(36)]
    public string TerminalId { get; set; } = null!;
    public PaymentTerminal Terminal { get; set; } = null!;

    [Required]
    public PaymentSessionStatus Status { get; set; } = PaymentSessionStatus.Pending;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? ExternalRef { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
