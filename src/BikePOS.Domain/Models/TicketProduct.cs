using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikePOS.Models;

public class TicketProduct
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(36)]
    public string ServiceTicketId { get; set; } = null!;
    public ServiceTicket ServiceTicket { get; set; } = null!;

    [Required]
    [MaxLength(36)]
    public string ProductId { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitPrice { get; set; }
}
