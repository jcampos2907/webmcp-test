using Microsoft.AspNetCore.DataProtection;

namespace BikePOS.Services;

/// <summary>
/// Encrypts/decrypts sensitive configuration values using ASP.NET Data Protection API.
/// Keys are managed automatically and stored in the app's data protection key ring.
/// </summary>
public class SecretProtector
{
    private const string Purpose = "BikePOS.OidcSecrets.v1";
    private readonly IDataProtector _protector;

    public SecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return "";
        return _protector.Protect(plaintext);
    }

    public string? Unprotect(string? ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return null;
        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch
        {
            // Corrupted or key-rotated — return null so the UI prompts re-entry
            return null;
        }
    }
}
