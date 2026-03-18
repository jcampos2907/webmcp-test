using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class AppUser
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(500)]
    public string ExternalSubjectId { get; set; } = "";

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<StoreUser> StoreUsers { get; set; } = new();
}
