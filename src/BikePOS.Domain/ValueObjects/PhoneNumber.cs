using System.Text.RegularExpressions;
using BikePOS.Domain.Common;

namespace BikePOS.Domain.ValueObjects;

/// <summary>
/// Phone number value object. Stores normalized digits and optional original formatting.
/// </summary>
public partial class PhoneNumber : ValueObject
{
    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber? Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = DigitsOnly().Replace(value.Trim(), "");
        if (cleaned.Length < 7 || cleaned.Length > 15)
            throw new ArgumentException($"Phone number must be 7–15 digits: {value}", nameof(value));

        return new PhoneNumber(value.Trim());
    }

    [GeneratedRegex(@"[^\d+]")]
    private static partial Regex DigitsOnly();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;
}
