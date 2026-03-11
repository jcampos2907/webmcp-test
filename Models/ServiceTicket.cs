using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public enum TicketStatus
{
    Open,
    InProgress,
    WaitingForParts,
    Completed,
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
    public int BikeId { get; set; }
    public Bike Bike { get; set; } = null!;

    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    // Placeholder: will reference a Mechanic model in the future
    public int? MechanicId { get; set; }

    // Placeholder: will reference a Service model in the future
    public int? BaseServiceId { get; set; }

    // Placeholder: will reference Product model(s) in the future (additional parts)
    // public ICollection<Product> AdditionalParts { get; set; }

    [Required]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }
}
