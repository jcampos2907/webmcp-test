using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class StoreUser
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(36)]
    public string AppUserId { get; set; } = null!;
    public AppUser AppUser { get; set; } = null!;

    [MaxLength(36)]
    public string StoreId { get; set; } = null!;
    public Store Store { get; set; } = null!;

    public StoreRole Role { get; set; } = StoreRole.Cashier;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
