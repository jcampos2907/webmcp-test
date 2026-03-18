using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public class Customer
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = null!;

    [Phone]
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Street { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public int? StoreId { get; set; }
    public Store? Store { get; set; }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    public ICollection<Component> Components { get; set; } = new List<Component>();
    public ICollection<CustomerMetaValue> MetaValues { get; set; } = new List<CustomerMetaValue>();
}
