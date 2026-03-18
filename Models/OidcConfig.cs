using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

/// <summary>
/// Stores OIDC provider configuration per conglomerate.
/// Only one active config per conglomerate. Managed by SuperAdmin/Developer.
/// </summary>
public class OidcConfig
{
    public int Id { get; set; }

    public int ConglomerateId { get; set; }
    public Conglomerate Conglomerate { get; set; } = null!;

    [Required, MaxLength(500)]
    public string Authority { get; set; } = "";

    [Required, MaxLength(200)]
    public string ClientId { get; set; } = "";

    /// <summary>Stored encrypted or hashed in production. Displayed masked in UI.</summary>
    [MaxLength(500)]
    public string? ClientSecret { get; set; }

    [MaxLength(100)]
    public string ResponseType { get; set; } = "code";

    /// <summary>Space-separated OIDC scopes (e.g. "openid profile email")</summary>
    [MaxLength(500)]
    public string Scopes { get; set; } = "openid profile email";

    public bool MapInboundClaims { get; set; } = false;

    public bool SaveTokens { get; set; } = true;

    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;

    /// <summary>Display name for this provider (e.g. "Keycloak", "Azure AD")</summary>
    [MaxLength(200)]
    public string? ProviderName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? UpdatedBy { get; set; }
}
