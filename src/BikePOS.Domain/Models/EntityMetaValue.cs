using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

/// <summary>
/// Generic meta value storage for any entity type (Company, Conglomerate, Store).
/// CustomerMetaValue remains separate for backward compatibility.
/// </summary>
public class EntityMetaValue
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(50)]
    public string EntityType { get; set; } = "";

    /// <summary>Primary key (UUID) of the target entity</summary>
    [MaxLength(36)]
    public string EntityId { get; set; } = "";

    [MaxLength(36)]
    public string MetaFieldDefinitionId { get; set; } = null!;
    public MetaFieldDefinition MetaFieldDefinition { get; set; } = null!;

    [MaxLength(500)]
    public string? Value { get; set; }
}
