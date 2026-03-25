using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public enum PaymentMethod
{
    Cash,
    Card,
    Transfer,
    Mixed,
    Pending
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Cancelled,
    Failed
}

public class Charge
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(36)]
    public string ServiceTicketId { get; set; } = null!;
    public ServiceTicket ServiceTicket { get; set; } = null!;

    [Required]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    public DateTime ChargedAt { get; set; } = DateTime.UtcNow;

    public string? CashierName { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pending;

    public string? ExternalTransactionId { get; set; }

    public string? Notes { get; set; }

    [Required]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public DateTime? CompletedAt { get; set; }

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }

    [MaxLength(500)]
    public string? CreatedBy { get; set; }

    // ERP sync
    [MaxLength(200)]
    public string? ExternalId { get; set; }
    [MaxLength(100)]
    public string? ExternalSource { get; set; }
}
