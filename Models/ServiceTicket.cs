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
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    [Required]
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    public int? MechanicId { get; set; }
    public Mechanic? Mechanic { get; set; }

    public int? BaseServiceId { get; set; }
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
}
