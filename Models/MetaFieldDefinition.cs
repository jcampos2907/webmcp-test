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

    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    [MaxLength(1000)]
    public string? Options { get; set; }

    /// <summary>JS event that triggers the action: "blur", "input", "change"</summary>
    [MaxLength(20)]
    public string? ActionEvent { get; set; }

    /// <summary>JavaScript function body. Receives `value` param, returns transformed value. E.g.: return value.replace(/\D/g, '')</summary>
    [MaxLength(2000)]
    public string? ActionScript { get; set; }

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

    /// <summary>
    /// Returns per-country preset field definitions for a given entity type.
    /// Each call returns new instances (new IDs) so they can be inserted fresh.
    /// </summary>
    public static List<MetaFieldDefinition> GetPresetsForCountry(string countryCode, string entityType)
    {
        if (countryCode == "CR" && entityType == "Customer")
        {
            var tipoPersona = new MetaFieldDefinition
            {
                Key = "tipo_persona", Label = "Tipo de Persona", FieldType = "select", IsRequired = false, SortOrder = 0, IsActive = true,
                EntityType = "Customer",
                Options = "Física, Jurídica"
            };
            return new()
            {
                tipoPersona,
                new MetaFieldDefinition
                {
                    Key = "cedula", Label = "Cédula", FieldType = "text", IsRequired = false, SortOrder = 1, IsActive = true,
                    EntityType = "Customer",
                    RegexPattern = @"^\d-\d{4}-\d{4}$", RegexMessage = "Formato: 9-9999-9999",
                    FormatMask = "9-9999-9999",
                    ConditionalOnFieldId = tipoPersona.Id,
                    ConditionalOnValue = "Física"
                },
                new MetaFieldDefinition
                {
                    Key = "cedula_juridica", Label = "Cédula Jurídica", FieldType = "text", IsRequired = false, SortOrder = 2, IsActive = true,
                    EntityType = "Customer",
                    RegexPattern = @"^\d-\d{3}-\d{6}$", RegexMessage = "Formato: 9-999-999999",
                    FormatMask = "9-999-999999",
                    ConditionalOnFieldId = tipoPersona.Id,
                    ConditionalOnValue = "Jurídica"
                }
            };
        }
        return new();
    }
}
