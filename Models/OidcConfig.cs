using System.ComponentModel.DataAnnotations;

namespace BikePOS.Models;

public class OidcConfig
{
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(36)]
    public string ConglomerateId { get; set; } = null!;
    public Conglomerate Conglomerate { get; set; } = null!;

    [Required, MaxLength(500)]
    public string Authority { get; set; } = "";

    [Required, MaxLength(200)]
    public string ClientId { get; set; } = "";

    [MaxLength(500)]
    public string? ClientSecret { get; set; }

    [MaxLength(100)]
    public string ResponseType { get; set; } = "code";

    [MaxLength(500)]
    public string Scopes { get; set; } = "openid profile email";

    public bool MapInboundClaims { get; set; } = false;
    public bool SaveTokens { get; set; } = true;
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;

    [MaxLength(200)]
    public string? ProviderName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? UpdatedBy { get; set; }
}
