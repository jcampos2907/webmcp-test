namespace BikePOS.Models;

public record FormBlock(string Key, string LabelKey, bool IsFixed, int DisplayOrder);

public static class FormBlockDefinition
{
    public static List<FormBlock> GetBlocks(string entityType) => entityType switch
    {
        "ServiceTicket" => new()
        {
            new("header", "Settings_Block_Header", IsFixed: true, DisplayOrder: 0),
            new("details", "Settings_Block_Details", IsFixed: false, DisplayOrder: 1),
            new("products", "Settings_Block_Products", IsFixed: true, DisplayOrder: 2),
            new("summary", "Settings_Block_Summary", IsFixed: false, DisplayOrder: 3),
            new("totals", "Settings_Block_Totals", IsFixed: true, DisplayOrder: 4),
        },
        "Customer" => new()
        {
            new("info", "Settings_Block_Info", IsFixed: false, DisplayOrder: 0),
            new("address", "Settings_Block_Address", IsFixed: false, DisplayOrder: 1),
        },
        "Component" => new()
        {
            new("details", "Settings_Block_Details", IsFixed: false, DisplayOrder: 0),
        },
        _ => new() { new("details", "Settings_Block_Details", IsFixed: false, DisplayOrder: 0) },
    };

    public static string DefaultBlock(string entityType) => entityType switch
    {
        "Customer" => "info",
        "ServiceTicket" => "details",
        _ => "details"
    };
}
