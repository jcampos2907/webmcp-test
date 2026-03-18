using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class MetaFieldDefinition
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Which entity type this field applies to: Customer, Company, Conglomerate, Store</summary>
    [Required, MaxLength(50)]
    public string EntityType { get; set; } = "Customer";

    [Required, MaxLength(100)]
    public string Key { get; set; } = null!;

    [Required, MaxLength(150)]
    public string Label { get; set; } = null!;

    [MaxLength(50)]
    public string FieldType { get; set; } = "text";

    public bool IsRequired { get; set; } = false;

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? RegexPattern { get; set; }

    [MaxLength(200)]
    public string? RegexMessage { get; set; }

    [MaxLength(100)]
    public string? FormatMask { get; set; }

    [MaxLength(1000)]
    public string? Options { get; set; }

    [MaxLength(36)]
    public string? ConditionalOnFieldId { get; set; }
    public MetaFieldDefinition? ConditionalOnField { get; set; }

    [MaxLength(200)]
    public string? ConditionalOnValue { get; set; }

    [MaxLength(36)]
    public string? CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }
}
