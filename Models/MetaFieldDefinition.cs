using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class MetaFieldDefinition
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Key { get; set; } = null!;

    [Required, MaxLength(150)]
    public string Label { get; set; } = null!;

    [MaxLength(50)]
    public string FieldType { get; set; } = "text"; // text, number, email, tel, date, url, select

    public bool IsRequired { get; set; } = false;

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    /// <summary>Regex pattern for input validation (e.g. ^\d \d{4} \d{4}$ for CR tax IDs)</summary>
    [MaxLength(500)]
    public string? RegexPattern { get; set; }

    /// <summary>User-facing error message when regex validation fails</summary>
    [MaxLength(200)]
    public string? RegexMessage { get; set; }

    /// <summary>Display format mask hint shown as placeholder (e.g. "0 0000 0000")</summary>
    [MaxLength(100)]
    public string? FormatMask { get; set; }

    /// <summary>Comma-separated options for select fields (e.g. "Persona Física,Persona Jurídica")</summary>
    [MaxLength(1000)]
    public string? Options { get; set; }

    /// <summary>If set, this field only appears when the referenced field has the specified value</summary>
    public int? ConditionalOnFieldId { get; set; }
    public MetaFieldDefinition? ConditionalOnField { get; set; }

    /// <summary>The value the parent field must have for this field to be visible</summary>
    [MaxLength(200)]
    public string? ConditionalOnValue { get; set; }

    public int? StoreId { get; set; }
    public Store? Store { get; set; }
}
