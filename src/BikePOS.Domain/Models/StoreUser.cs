using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public enum RoleScope
{
    Store,
    Company,
    Conglomerate
}

public class StoreUser
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(36)]
    public string AppUserId { get; set; } = null!;
    public AppUser AppUser { get; set; } = null!;

    public RoleScope Scope { get; set; } = RoleScope.Store;

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }

    [MaxLength(36)]
    public string? CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(36)]
    public string? ConglomerateId { get; set; }
    public Conglomerate? Conglomerate { get; set; }

    public StoreRole Role { get; set; } = StoreRole.Cashier;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
