using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public enum NotificationChannel
{
    Email,
    WhatsApp
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}

public class NotificationLog
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(36)]
    public string ServiceTicketId { get; set; } = null!;
    public ServiceTicket ServiceTicket { get; set; } = null!;

    [MaxLength(36)]
    public string? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    [MaxLength(500)]
    public string? Recipient { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }
}
