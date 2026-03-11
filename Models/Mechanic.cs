using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class Mechanic
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;
}
