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
    public int Id { get; set; }

    public int ChargeId { get; set; }
    public Charge Charge { get; set; } = null!;

    public int TerminalId { get; set; }
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
