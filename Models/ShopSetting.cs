using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class ShopSetting
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(100)]
    public string Key { get; set; } = null!;

    [MaxLength(500)]
    public string Value { get; set; } = "";

    [MaxLength(36)]
    public string? StoreId { get; set; }
    public Store? Store { get; set; }
}
