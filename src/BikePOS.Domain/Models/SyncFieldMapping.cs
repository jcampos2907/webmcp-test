using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class SyncFieldMapping
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(36)]
    public string ErpConnectionId { get; set; } = null!;
    public ErpConnection ErpConnection { get; set; } = null!;

    [Required, MaxLength(50)]
    public string EntityType { get; set; } = null!;

    [Required, MaxLength(100)]
    public string LocalField { get; set; } = null!;

    [Required, MaxLength(100)]
    public string RemoteField { get; set; } = null!;

    /// <summary>Optional transform expression (e.g. "toUpper", "concat:FirstName,LastName")</summary>
    [MaxLength(500)]
    public string? TransformExpression { get; set; }

    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}
