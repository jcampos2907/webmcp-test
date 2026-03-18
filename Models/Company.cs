using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class Company
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(36)]
    public string ConglomerateId { get; set; } = null!;
    public Conglomerate Conglomerate { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(20)]
    public string Locale { get; set; } = "es-CR";

    [MaxLength(10)]
    public string Currency { get; set; } = "CRC";

    [MaxLength(100)]
    public string? TaxId { get; set; }

    [MaxLength(2)]
    public string? CountryCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Store> Stores { get; set; } = new();
}
