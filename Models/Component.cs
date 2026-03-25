using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public class Component
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string? Name { get; set; }

    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "SKU must be alphanumeric and uppercase.")]
    public string Sku { get; set; } = null!;

    public string Color { get; set; } = null!;

    public string Brand { get; set; } = null!;

    [MaxLength(50)]
    public string ComponentType { get; set; } = "Bicicleta";

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    [MaxLength(36)]
    public string? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }

    // ERP sync
    [MaxLength(200)]
    public string? ExternalId { get; set; }
    [MaxLength(100)]
    public string? ExternalSource { get; set; }
}
