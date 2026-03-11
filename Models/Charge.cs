using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public enum PaymentMethod
{
    Cash,
    Card,
    Pending
}

public class Charge
{
    public int Id { get; set; }

    [Required]
    public int ServiceTicketId { get; set; }
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
}
