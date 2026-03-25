using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class ErpConnection
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Provider { get; set; } = "generic_webhook";

    [MaxLength(500)]
    public string? BaseUrl { get; set; }

    [MaxLength(500)]
    public string? ApiKey { get; set; }

    public bool IsActive { get; set; }

    // Entity sync toggles
    public bool SyncCustomers { get; set; } = true;
    public bool SyncProducts { get; set; } = true;
    public bool SyncTickets { get; set; } = true;
    public bool SyncCharges { get; set; } = true;
    public bool SyncComponents { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }

    public ICollection<SyncFieldMapping> FieldMappings { get; set; } = new List<SyncFieldMapping>();
}
