using System.Text.RegularExpressions;
using BikePOS.Domain.Common;

namespace BikePOS.Domain.ValueObjects;

/// <summary>
/// Stock-keeping unit identifier. Validated to be uppercase alphanumeric with optional dashes.
/// </summary>
public partial class Sku : ValueObject
{
    public string Value { get; }

    private Sku(string value)
    {
        Value = value;
    }

    public static Sku? Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var upper = value.Trim().ToUpperInvariant();
        if (!SkuRegex().IsMatch(upper))
            throw new ArgumentException($"SKU must be alphanumeric (dashes allowed): {value}", nameof(value));

        return new Sku(upper);
    }

    [GeneratedRegex(@"^[A-Z0-9\-]+$")]
    private static partial Regex SkuRegex();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Sku sku) => sku.Value;
}
