using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class CustomerMetaValue
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int MetaFieldDefinitionId { get; set; }
    public MetaFieldDefinition MetaFieldDefinition { get; set; } = null!;

    [MaxLength(500)]
    public string? Value { get; set; }
}
