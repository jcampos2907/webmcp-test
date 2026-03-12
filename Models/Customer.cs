using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class Customer
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public ICollection<Bike> Bikes { get; set; } = new List<Bike>();
}
