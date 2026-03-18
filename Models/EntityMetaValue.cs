using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

/// <summary>
/// Generic meta value storage for any entity type (Company, Conglomerate, Store).
/// CustomerMetaValue remains separate for backward compatibility.
/// </summary>
public class EntityMetaValue
{
    public int Id { get; set; }

    /// <summary>Entity type matching MetaFieldDefinition.EntityType (e.g. "Company", "Store")</summary>
    [Required, MaxLength(50)]
    public string EntityType { get; set; } = "";

    /// <summary>Primary key of the target entity</summary>
    public int EntityId { get; set; }

    public int MetaFieldDefinitionId { get; set; }
    public MetaFieldDefinition MetaFieldDefinition { get; set; } = null!;

    [MaxLength(500)]
    public string? Value { get; set; }
}
