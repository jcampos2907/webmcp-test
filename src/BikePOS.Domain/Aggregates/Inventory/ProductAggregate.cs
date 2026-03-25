using BikePOS.Domain.Common;
using BikePOS.Domain.Aggregates.Inventory.Events;
using BikePOS.Domain.ValueObjects;

namespace BikePOS.Domain.Aggregates.Inventory;

/// <summary>
/// Product aggregate root. Manages inventory tracking with stock decrement/restore
/// and low-stock detection.
/// </summary>
public class ProductAggregate : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public string? Sku { get; private set; }
    public decimal Price { get; private set; }
    public int QuantityInStock { get; private set; }
    public string? Category { get; private set; }
    public string? StoreId { get; private set; }

    private ProductAggregate() { }

    public static ProductAggregate Create(
        string name,
        string? sku,
        decimal price,
        int quantityInStock,
        string? category,
        string? storeId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (quantityInStock < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityInStock), "Stock cannot be negative.");

        // Validate SKU format if provided
        if (!string.IsNullOrWhiteSpace(sku))
            ValueObjects.Sku.Create(sku);

        return new ProductAggregate
        {
            Name = name.Trim(),
            Sku = sku?.Trim().ToUpperInvariant(),
            Price = price,
            QuantityInStock = quantityInStock,
            Category = category?.Trim(),
            StoreId = storeId
        };
    }

    public static ProductAggregate Reconstitute(
        string id, string name, string? sku, decimal price,
        int quantityInStock, string? category, string? storeId)
    {
        var product = new ProductAggregate
        {
            Name = name,
            Sku = sku,
            Price = price,
            QuantityInStock = quantityInStock,
            Category = category,
            StoreId = storeId
        };
        product.Id = id;
        return product;
    }

    // ── Stock management ────────────────────────────────────────────

    /// <summary>Decrement stock when product is added to a ticket.</summary>
    public void DecrementStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (QuantityInStock < quantity)
            throw new InvalidOperationException(
                $"Insufficient stock for {Name}. Available: {QuantityInStock}, requested: {quantity}.");

        QuantityInStock -= quantity;

        if (QuantityInStock <= LowStockThreshold)
        {
            AddDomainEvent(new LowStockEvent(Id, Name, QuantityInStock, StoreId));
        }
    }

    /// <summary>Restore stock when a product is removed from a ticket or ticket is cancelled.</summary>
    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        QuantityInStock += quantity;
    }

    /// <summary>Set stock to a specific value (e.g. from ERP sync).</summary>
    public void SetStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock cannot be negative.");

        QuantityInStock = quantity;
    }

    // ── Product info ────────────────────────────────────────────────

    public void UpdateInfo(string name, string? sku, decimal price, string? category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");

        if (!string.IsNullOrWhiteSpace(sku))
            ValueObjects.Sku.Create(sku);

        Name = name.Trim();
        Sku = sku?.Trim().ToUpperInvariant();
        Price = price;
        Category = category?.Trim();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    public bool IsLowStock => QuantityInStock <= LowStockThreshold;
    public bool IsOutOfStock => QuantityInStock <= 0;

    /// <summary>Configurable threshold — could be pulled from settings in the future.</summary>
    private const int LowStockThreshold = 5;
}
