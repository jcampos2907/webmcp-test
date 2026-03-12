using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class ShopSetting
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Key { get; set; } = null!;

    [MaxLength(500)]
    public string Value { get; set; } = "";
}
