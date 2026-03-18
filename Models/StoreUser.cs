using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class StoreUser
{
    public int Id { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public StoreRole Role { get; set; } = StoreRole.Cashier;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
