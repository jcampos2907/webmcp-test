using BikePOS.Domain.Common;
using BikePOS.Domain.Aggregates.ServiceTicket.Events;

namespace BikePOS.Domain.Aggregates.ServiceTicket;

/// <summary>
/// ServiceTicket aggregate root. Encapsulates the full ticket lifecycle:
/// creation, status transitions, product management, discount calculation,
/// charge processing, refunds, and cancellation.
///
/// This is a domain model — it does NOT depend on EF Core, Blazor, or HTTP.
/// Persistence models in Models/ remain for EF Core mapping.
/// </summary>
public class ServiceTicketAggregate : AggregateRoot
{
    public int TicketNumber { get; private set; }
    public string TicketDisplay => $"T-{TicketNumber:D3}";
    public TicketStatus Status { get; private set; } = TicketStatus.Open;
    public string ComponentId { get; private set; } = null!;
    public string? CustomerId { get; private set; }
    public string? MechanicId { get; private set; }
    public string? BaseServiceId { get; private set; }
    public decimal BaseServicePrice { get; private set; }
    public string? Description { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public string? StoreId { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<LineItem> _lineItems = new();
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    private readonly List<ChargeRecord> _charges = new();
    public IReadOnlyList<ChargeRecord> Charges => _charges.AsReadOnly();

    // Private constructor for reconstitution from persistence
    private ServiceTicketAggregate() { }

    /// <summary>
    /// Factory method: creates a new service ticket.
    /// </summary>
    public static ServiceTicketAggregate Create(
        string componentId,
        string? customerId,
        string? mechanicId,
        string? baseServiceId,
        decimal baseServicePrice,
        string? description,
        decimal discountPercent,
        int ticketNumber,
        string? storeId,
        string? createdBy)
    {
        if (string.IsNullOrWhiteSpace(componentId))
            throw new ArgumentException("Component is required.", nameof(componentId));
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Discount must be 0–100.");
        if (ticketNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(ticketNumber), "Ticket number must be positive.");

        var ticket = new ServiceTicketAggregate
        {
            ComponentId = componentId,
            CustomerId = customerId,
            MechanicId = mechanicId,
            BaseServiceId = baseServiceId,
            BaseServicePrice = baseServicePrice,
            Description = description,
            DiscountPercent = discountPercent,
            TicketNumber = ticketNumber,
            StoreId = storeId,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ticket.AddDomainEvent(new TicketCreatedEvent(
            ticket.Id, componentId, customerId, mechanicId, storeId));

        return ticket;
    }

    /// <summary>
    /// Reconstitute an aggregate from persistence data (no events raised).
    /// </summary>
    public static ServiceTicketAggregate Reconstitute(
        string id,
        int ticketNumber,
        TicketStatus status,
        string componentId,
        string? customerId,
        string? mechanicId,
        string? baseServiceId,
        decimal baseServicePrice,
        string? description,
        decimal discountPercent,
        string? storeId,
        string? createdBy,
        string? updatedBy,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<LineItem> lineItems,
        IEnumerable<ChargeRecord> charges)
    {
        var ticket = new ServiceTicketAggregate
        {
            TicketNumber = ticketNumber,
            Status = status,
            ComponentId = componentId,
            CustomerId = customerId,
            MechanicId = mechanicId,
            BaseServiceId = baseServiceId,
            BaseServicePrice = baseServicePrice,
            Description = description,
            DiscountPercent = discountPercent,
            StoreId = storeId,
            CreatedBy = createdBy,
            UpdatedBy = updatedBy,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        ticket.Id = id;
        ticket._lineItems.AddRange(lineItems);
        ticket._charges.AddRange(charges);
        return ticket;
    }

    // ── Status transitions ──────────────────────────────────────────

    public void ChangeStatus(TicketStatus newStatus, string? changedBy)
    {
        if (Status == newStatus) return;

        if (!TicketStatusTransitions.CanTransition(Status, newStatus))
            throw new InvalidOperationException(
                $"Cannot transition from {Status} to {newStatus}.");

        var old = Status;
        Status = newStatus;
        Touch(changedBy);

        AddDomainEvent(new TicketStatusChangedEvent(Id, old, newStatus, changedBy));
    }

    public bool IsReadOnly => Status is TicketStatus.Charged or TicketStatus.Cancelled;

    // ── Mechanic assignment ─────────────────────────────────────────

    public void AssignMechanic(string? mechanicId, string? changedBy)
    {
        GuardNotReadOnly();
        MechanicId = mechanicId;
        Touch(changedBy);
    }

    // ── Description ─────────────────────────────────────────────────

    public void UpdateDescription(string? description, string? changedBy)
    {
        GuardNotReadOnly();
        Description = description;
        Touch(changedBy);
    }

    // ── Discount ────────────────────────────────────────────────────

    public void ApplyDiscount(decimal percent, string? changedBy)
    {
        GuardNotReadOnly();
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Discount must be 0–100.");

        DiscountPercent = percent;
        Touch(changedBy);
    }

    // ── Line items (products on ticket) ─────────────────────────────

    public void AddProduct(string productId, string productName, decimal unitPrice, int quantity, string? changedBy)
    {
        GuardNotReadOnly();
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        var existing = _lineItems.FirstOrDefault(li => li.ProductId == productId);
        if (existing != null)
        {
            existing.IncreaseQuantity(quantity);
        }
        else
        {
            _lineItems.Add(new LineItem(productId, productName, unitPrice, quantity));
        }
        Touch(changedBy);
    }

    public void RemoveProduct(string productId, string? changedBy)
    {
        GuardNotReadOnly();
        var item = _lineItems.FirstOrDefault(li => li.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException($"Product {productId} not on this ticket.");

        _lineItems.Remove(item);
        Touch(changedBy);
    }

    /// <summary>
    /// Returns the inventory adjustments needed when saving. Each entry is (productId, qtyToDecrement).
    /// Positive = decrement stock, negative = restore stock.
    /// </summary>
    public List<(string ProductId, int QuantityDelta)> CalculateInventoryDelta(
        IReadOnlyList<LineItem> previousLineItems)
    {
        var deltas = new List<(string, int)>();

        // Products removed → restore inventory
        foreach (var prev in previousLineItems)
        {
            var current = _lineItems.FirstOrDefault(li => li.ProductId == prev.ProductId);
            if (current == null)
            {
                deltas.Add((prev.ProductId, -prev.Quantity)); // restore all
            }
            else if (current.Quantity < prev.Quantity)
            {
                deltas.Add((prev.ProductId, -(prev.Quantity - current.Quantity))); // restore difference
            }
            else if (current.Quantity > prev.Quantity)
            {
                deltas.Add((prev.ProductId, current.Quantity - prev.Quantity)); // decrement more
            }
        }

        // Newly added products → decrement inventory
        foreach (var item in _lineItems)
        {
            if (previousLineItems.All(p => p.ProductId != item.ProductId))
            {
                deltas.Add((item.ProductId, item.Quantity));
            }
        }

        return deltas;
    }

    // ── Pricing ─────────────────────────────────────────────────────

    public decimal Subtotal => BaseServicePrice + _lineItems.Sum(li => li.TotalPrice);

    public decimal Total => Math.Round(Subtotal * (1 - DiscountPercent / 100m), 2);

    public decimal TotalCharged => _charges
        .Where(c => c.PaymentStatus == PaymentStatus.Completed)
        .Sum(c => c.Amount);

    public decimal RemainingBalance => Total - TotalCharged;

    public bool IsFullyPaid => TotalCharged >= Total && Total > 0;

    // ── Charging ────────────────────────────────────────────────────

    public ChargeRecord ProcessCharge(
        decimal amount,
        PaymentMethod paymentMethod,
        string? cashierName,
        string? storeId)
    {
        if (Status == TicketStatus.Cancelled)
            throw new InvalidOperationException("Cannot charge a cancelled ticket.");
        if (Status == TicketStatus.Charged)
            throw new InvalidOperationException("Ticket is already fully charged.");
        if (Total <= 0)
            throw new InvalidOperationException("Cannot charge a ticket with $0 total.");
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Charge amount must be positive.");

        // Cap at remaining balance
        var chargeAmount = Math.Min(amount, RemainingBalance);

        var charge = new ChargeRecord(
            Guid.NewGuid().ToString(),
            Id,
            chargeAmount,
            paymentMethod,
            cashierName,
            storeId);

        _charges.Add(charge);

        // Check if fully paid after this charge
        if (TotalCharged >= Total)
        {
            var oldStatus = Status;
            Status = TicketStatus.Charged;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new TicketChargedEvent(Id, charge.Id, chargeAmount, cashierName, storeId));
            if (oldStatus != TicketStatus.Charged)
            {
                AddDomainEvent(new TicketStatusChangedEvent(Id, oldStatus, TicketStatus.Charged, cashierName));
            }
        }

        return charge;
    }

    // ── Refund ──────────────────────────────────────────────────────

    public ChargeRecord ProcessRefund(
        decimal refundAmount,
        PaymentMethod paymentMethod,
        string? cashierName,
        string? storeId)
    {
        if (refundAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(refundAmount), "Refund amount must be positive.");
        if (refundAmount > TotalCharged)
            throw new InvalidOperationException("Refund amount exceeds total charged.");

        var refund = ChargeRecord.CreateRefund(
            Guid.NewGuid().ToString(),
            Id,
            refundAmount,
            paymentMethod,
            cashierName,
            storeId);

        _charges.Add(refund);

        // Reopen ticket after refund
        if (Status == TicketStatus.Charged)
        {
            var old = Status;
            Status = TicketStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new TicketStatusChangedEvent(Id, old, TicketStatus.Completed, cashierName));
        }

        return refund;
    }

    // ── Cancellation ────────────────────────────────────────────────

    /// <summary>
    /// Cancel the ticket. Returns the list of (productId, quantity) to restore to inventory.
    /// </summary>
    public List<(string ProductId, int Quantity)> Cancel(string? cancelledBy)
    {
        if (Status == TicketStatus.Cancelled)
            throw new InvalidOperationException("Ticket is already cancelled.");

        var inventoryToRestore = _lineItems
            .Select(li => (li.ProductId, li.Quantity))
            .ToList();

        var oldStatus = Status;
        Status = TicketStatus.Cancelled;
        Touch(cancelledBy);

        AddDomainEvent(new TicketStatusChangedEvent(Id, oldStatus, TicketStatus.Cancelled, cancelledBy));

        return inventoryToRestore;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private void Touch(string? changedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = changedBy;
    }

    private void GuardNotReadOnly()
    {
        if (IsReadOnly)
            throw new InvalidOperationException($"Ticket is {Status} and cannot be modified.");
    }
}

// ── Line Item (child entity within the aggregate) ───────────────────

public class LineItem
{
    public string ProductId { get; }
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; private set; }
    public decimal TotalPrice => UnitPrice * Quantity;

    public LineItem(string productId, string productName, decimal unitPrice, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        Quantity += amount;
    }
}

// ── Charge Record (child entity within the aggregate) ───────────────

public enum PaymentMethod
{
    Cash,
    Card,
    Transfer,
    Mixed,
    Pending
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Cancelled,
    Failed
}

public class ChargeRecord
{
    public string Id { get; }
    public string ServiceTicketId { get; }
    public decimal Amount { get; }
    public PaymentMethod PaymentMethod { get; }
    public PaymentStatus PaymentStatus { get; private set; }
    public string? CashierName { get; }
    public string? StoreId { get; }
    public DateTime ChargedAt { get; }
    public DateTime? CompletedAt { get; private set; }
    public string? Notes { get; private set; }

    public ChargeRecord(
        string id,
        string serviceTicketId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? cashierName,
        string? storeId)
    {
        Id = id;
        ServiceTicketId = serviceTicketId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        PaymentStatus = paymentMethod is PaymentMethod.Cash or PaymentMethod.Transfer
            ? PaymentStatus.Completed
            : PaymentStatus.Pending;
        CashierName = cashierName;
        StoreId = storeId;
        ChargedAt = DateTime.UtcNow;
        CompletedAt = PaymentStatus == PaymentStatus.Completed ? DateTime.UtcNow : null;
    }

    public static ChargeRecord CreateRefund(
        string id,
        string serviceTicketId,
        decimal refundAmount,
        PaymentMethod paymentMethod,
        string? cashierName,
        string? storeId)
    {
        var refund = new ChargeRecord(id, serviceTicketId, -refundAmount, paymentMethod, cashierName, storeId);
        refund.PaymentStatus = PaymentStatus.Completed;
        refund.CompletedAt = DateTime.UtcNow;
        refund.Notes = "Refund";
        return refund;
    }

    public void MarkCompleted()
    {
        PaymentStatus = PaymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        PaymentStatus = PaymentStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkCancelled()
    {
        PaymentStatus = PaymentStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }
}
