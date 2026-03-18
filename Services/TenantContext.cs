using System.Security.Claims;
using BikePOS.Models;

namespace BikePOS.Services;

/// <summary>
/// Scoped service that holds the current user's tenant context.
/// Populated from claims that are set during OIDC OnTokenValidated.
/// </summary>
public class TenantContext
{
    public int? AppUserId { get; private set; }
    public int? StoreId { get; private set; }
    public string? StoreName { get; private set; }
    public int? CompanyId { get; private set; }
    public string? CompanyName { get; private set; }
    public int? ConglomerateId { get; private set; }
    public StoreRole? Role { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Email { get; private set; }
    public string? ExternalSubjectId { get; private set; }
    public string? PreferredUsername { get; private set; }

    /// <summary>
    /// Stable user identifier for audit fields: "sub:{ExternalSubjectId}" (IdP subject).
    /// Falls back to "uid:{AppUserId}" or "unknown".
    /// </summary>
    public string UserIdentifier =>
        !string.IsNullOrEmpty(ExternalSubjectId) ? $"sub:{ExternalSubjectId}" :
        AppUserId.HasValue ? $"uid:{AppUserId}" :
        "unknown";

    public bool IsResolved => StoreId.HasValue;

    public void PopulateFromClaims(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return;

        if (int.TryParse(user.FindFirstValue("app_user_id"), out var uid))
            AppUserId = uid;
        if (int.TryParse(user.FindFirstValue("store_id"), out var sid))
            StoreId = sid;
        if (int.TryParse(user.FindFirstValue("company_id"), out var cid))
            CompanyId = cid;
        if (int.TryParse(user.FindFirstValue("conglomerate_id"), out var congId))
            ConglomerateId = congId;
        if (Enum.TryParse<StoreRole>(user.FindFirstValue("store_role"), out var role))
            Role = role;

        StoreName = user.FindFirstValue("store_name");
        CompanyName = user.FindFirstValue("company_name");
        DisplayName = user.FindFirstValue("name") ?? user.FindFirstValue("preferred_username");
        Email = user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email);
        ExternalSubjectId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        PreferredUsername = user.FindFirstValue("preferred_username");
    }
}
