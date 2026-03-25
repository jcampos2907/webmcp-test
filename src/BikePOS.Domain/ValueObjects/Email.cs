using System.Text.RegularExpressions;
using BikePOS.Domain.Common;

namespace BikePOS.Domain.ValueObjects;

/// <summary>
/// Validated email address value object.
/// </summary>
public partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email? Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim().ToLowerInvariant();
        if (!EmailRegex().IsMatch(trimmed))
            throw new ArgumentException($"Invalid email address: {value}", nameof(value));

        return new Email(trimmed);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
