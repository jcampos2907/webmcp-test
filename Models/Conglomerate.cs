using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class Conglomerate
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Company> Companies { get; set; } = new();
}
