using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class Mechanic
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }
}
