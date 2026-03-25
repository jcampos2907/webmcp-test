using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class BaseFieldLayout
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Which entity type: Customer, Component, ServiceTicket, Company, Store</summary>
    [Required, MaxLength(50)]
    public string EntityType { get; set; } = null!;

    /// <summary>The base field key, e.g. "FirstName", "Phone"</summary>
    [Required, MaxLength(100)]
    public string FieldKey { get; set; } = null!;

    /// <summary>Display label (localized key or plain text)</summary>
    [Required, MaxLength(150)]
    public string Label { get; set; } = null!;

    /// <summary>Which block/zone this field belongs to, e.g. "info", "details", "summary"</summary>
    [MaxLength(50)]
    public string Block { get; set; } = "details";

    public int SortOrder { get; set; } = 0;

    [MaxLength(36)]
    public string? CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }

    /// <summary>
    /// Returns the default base fields for a given entity type.
    /// These are auto-seeded on first load if no BaseFieldLayout records exist.
    /// </summary>
    public static List<BaseFieldLayout> GetBaseFields(string entityType)
    {
        return entityType switch
        {
            "Customer" => new()
            {
                new() { EntityType = "Customer", FieldKey = "FirstName", Label = "First Name", Block = "info", SortOrder = 0 },
                new() { EntityType = "Customer", FieldKey = "LastName", Label = "Last Name", Block = "info", SortOrder = 1 },
                new() { EntityType = "Customer", FieldKey = "Phone", Label = "Phone", Block = "info", SortOrder = 2 },
                new() { EntityType = "Customer", FieldKey = "Email", Label = "Email", Block = "info", SortOrder = 3 },
                new() { EntityType = "Customer", FieldKey = "Address", Label = "Address", Block = "address", SortOrder = 0 },
            },
            "Component" => new()
            {
                new() { EntityType = "Component", FieldKey = "Name", Label = "Name", Block = "details", SortOrder = 0 },
                new() { EntityType = "Component", FieldKey = "ComponentType", Label = "Type", Block = "details", SortOrder = 1 },
                new() { EntityType = "Component", FieldKey = "Brand", Label = "Brand", Block = "details", SortOrder = 2 },
                new() { EntityType = "Component", FieldKey = "Model", Label = "Model", Block = "details", SortOrder = 3 },
                new() { EntityType = "Component", FieldKey = "Color", Label = "Color", Block = "details", SortOrder = 4 },
                new() { EntityType = "Component", FieldKey = "SerialNumber", Label = "Serial Number", Block = "details", SortOrder = 5 },
                new() { EntityType = "Component", FieldKey = "Notes", Label = "Notes", Block = "details", SortOrder = 6 },
            },
            "ServiceTicket" => new()
            {
                new() { EntityType = "ServiceTicket", FieldKey = "Customer", Label = "Customer", Block = "header", SortOrder = 0 },
                new() { EntityType = "ServiceTicket", FieldKey = "Component", Label = "Component", Block = "header", SortOrder = 1 },
                new() { EntityType = "ServiceTicket", FieldKey = "Services", Label = "Services", Block = "header", SortOrder = 2 },
                new() { EntityType = "ServiceTicket", FieldKey = "Status", Label = "Status", Block = "details", SortOrder = 0 },
                new() { EntityType = "ServiceTicket", FieldKey = "Mechanic", Label = "Mechanic", Block = "details", SortOrder = 1 },
                new() { EntityType = "ServiceTicket", FieldKey = "Notes", Label = "Notes", Block = "summary", SortOrder = 0 },
                new() { EntityType = "ServiceTicket", FieldKey = "DiscountPercent", Label = "Discount %", Block = "summary", SortOrder = 1 },
            },
            "Company" => new()
            {
                new() { EntityType = "Company", FieldKey = "Name", Label = "Name", Block = "details", SortOrder = 0 },
                new() { EntityType = "Company", FieldKey = "Locale", Label = "Locale", Block = "details", SortOrder = 1 },
                new() { EntityType = "Company", FieldKey = "Currency", Label = "Currency", Block = "details", SortOrder = 2 },
                new() { EntityType = "Company", FieldKey = "TaxId", Label = "Tax ID", Block = "details", SortOrder = 3 },
                new() { EntityType = "Company", FieldKey = "CountryCode", Label = "Country Code", Block = "details", SortOrder = 4 },
            },
            "Store" => new()
            {
                new() { EntityType = "Store", FieldKey = "Name", Label = "Name", Block = "details", SortOrder = 0 },
                new() { EntityType = "Store", FieldKey = "Address", Label = "Address", Block = "details", SortOrder = 1 },
                new() { EntityType = "Store", FieldKey = "Phone", Label = "Phone", Block = "details", SortOrder = 2 },
                new() { EntityType = "Store", FieldKey = "Email", Label = "Email", Block = "details", SortOrder = 3 },
            },
            _ => new()
        };
    }
}
