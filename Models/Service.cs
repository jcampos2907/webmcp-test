using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public class Service
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal DefaultPrice { get; set; }

    public int? EstimatedMinutes { get; set; }

    public int? StoreId { get; set; }
    public Store? Store { get; set; }
}
