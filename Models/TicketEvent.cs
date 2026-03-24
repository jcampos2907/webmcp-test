using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public enum TicketEventType
{
    Created,
    StatusChanged,
    MechanicAssigned,
    NoteUpdated,
    ProductAdded,
    ProductRemoved,
    MetaFieldChanged,
    DiscountChanged,
    ChargeProcessed,
    Cancelled,
    Refunded
}

public class TicketEvent
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(36)]
    public string ServiceTicketId { get; set; } = null!;
    public ServiceTicket ServiceTicket { get; set; } = null!;

    [Required]
    public TicketEventType EventType { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>JSON blob for structured old/new values</summary>
    [MaxLength(2000)]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? CreatedBy { get; set; }

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }
}
