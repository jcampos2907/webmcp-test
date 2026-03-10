using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace webmcp.Models;

public class Bike
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string Sku { get; set; } = null!;

    public string Color { get; set; } = null!;

    public string Brand { get; set; } = null!;

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }
}