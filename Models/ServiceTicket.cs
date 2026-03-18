using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public enum TicketStatus
{
    Open,
    InProgress,
    WaitingForParts,
    Completed,
    Charged,
    Cancelled
}

public class ServiceTicket
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Store-scoped sequential number for human-readable display (e.g. T-001)</summary>
    public int TicketNumber { get; set; }

    [NotMapped]
    public string TicketDisplay => $"T-{TicketNumber:D3}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    [Required]
    [MaxLength(36)]
    public string ComponentId { get; set; } = null!;
    public Component Component { get; set; } = null!;

    [MaxLength(36)]
    public string? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    [MaxLength(36)]
    public string? MechanicId { get; set; }
    public Mechanic? Mechanic { get; set; }

    [MaxLength(36)]
    public string? BaseServiceId { get; set; }
    public Service? BaseService { get; set; }

    public ICollection<TicketProduct> TicketProducts { get; set; } = new List<TicketProduct>();

    public ICollection<Charge> Charges { get; set; } = new List<Charge>();

    [Required]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }

    [MaxLength(500)]
    public string? CreatedBy { get; set; }

    [MaxLength(500)]
    public string? UpdatedBy { get; set; }
}
