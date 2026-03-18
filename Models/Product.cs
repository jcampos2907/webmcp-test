using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "SKU must be alphanumeric and uppercase.")]
    public string? Sku { get; set; }

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    public int QuantityInStock { get; set; }

    public string? Category { get; set; }

    public int? StoreId { get; set; }
    public Store? Store { get; set; }
}
