namespace BikePOS.Application.DTOs;

public class TicketDetailsDto
{
    public string Id { get; set; } = null!;
    public int TicketNumber { get; set; }
    public string TicketDisplay { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ComponentName { get; set; }
    public string? ComponentType { get; set; }
    public string? CustomerName { get; set; }
    public string? MechanicName { get; set; }
    public string? ServiceName { get; set; }
    public decimal ServicePrice { get; set; }
    public string? Description { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public decimal TotalCharged { get; set; }
    public decimal RemainingBalance { get; set; }
    public bool IsFullyPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<TicketProductDto> Products { get; set; } = new();
    public List<ChargeDto> Charges { get; set; } = new();
    public List<TicketEventDto> Events { get; set; } = new();
}

public class TicketProductDto
{
    public string ProductId { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class ChargeDto
{
    public string Id { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public string? CashierName { get; set; }
    public DateTime ChargedAt { get; set; }
    public string? Notes { get; set; }
}

public class TicketEventDto
{
    public string EventType { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
