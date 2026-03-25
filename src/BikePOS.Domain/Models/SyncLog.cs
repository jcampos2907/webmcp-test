using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public enum SyncDirection { Outbound, Inbound }
public enum SyncStatus { Pending, Success, Failed, Skipped }

public class SyncLog
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(36)]
    public string ErpConnectionId { get; set; } = null!;
    public ErpConnection ErpConnection { get; set; } = null!;

    [Required]
    public SyncDirection Direction { get; set; }

    [Required, MaxLength(50)]
    public string EntityType { get; set; } = null!;

    [MaxLength(36)]
    public string? EntityId { get; set; }

    [Required]
    public SyncStatus Status { get; set; } = SyncStatus.Pending;

    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }
}
