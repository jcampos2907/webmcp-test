using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class CustomerMetaValue
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(36)]
    public string CustomerId { get; set; } = null!;
    public Customer Customer { get; set; } = null!;

    [MaxLength(36)]
    public string MetaFieldDefinitionId { get; set; } = null!;
    public MetaFieldDefinition MetaFieldDefinition { get; set; } = null!;

    [MaxLength(500)]
    public string? Value { get; set; }
}
