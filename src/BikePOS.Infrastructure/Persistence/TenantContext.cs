using System.Security.Claims;
using BikePOS.Models;

namespace BikePOS.Services;

/// <summary>
/// Scoped service that holds the current user's tenant context.
/// Populated from claims that are set during OIDC OnTokenValidated.
/// </summary>
public class TenantContext
{
    public string? AppUserId { get; private set; }
    public string? StoreId { get; private set; }
    public string? StoreName { get; private set; }
    public string? CompanyId { get; private set; }
    public string? CompanyName { get; private set; }
    public string? ConglomerateId { get; private set; }
    public StoreRole? Role { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Email { get; private set; }
    public string? ExternalSubjectId { get; private set; }
    public string? PreferredUsername { get; private set; }

    /// <summary>True when a SuperAdmin has manually switched store context.</summary>
    public bool IsOverridden { get; private set; }

    /// <summary>
    /// Stable user identifier for audit fields: "sub:{ExternalSubjectId}" (IdP subject).
    /// Falls back to "uid:{AppUserId}" or "unknown".
    /// </summary>
    public string UserIdentifier =>
        !string.IsNullOrEmpty(ExternalSubjectId) ? $"sub:{ExternalSubjectId}" :
        !string.IsNullOrEmpty(AppUserId) ? $"uid:{AppUserId}" :
        "unknown";

    public bool IsResolved => !string.IsNullOrEmpty(StoreId);

    /// <summary>
    /// Runtime context switch for SuperAdmins — overrides store/company/conglomerate
    /// without re-authenticating. Once set, PopulateFromClaims won't overwrite
    /// store/company/conglomerate fields.
    /// </summary>
    /// <summary>Sets the effective role for the active store (computed by MembershipResolver per request).</summary>
    public void SetRole(StoreRole role) => Role = role;

    public void SwitchContext(string storeId, string storeName, string companyId, string companyName, string conglomerateId)
    {
        StoreId = storeId;
        StoreName = storeName;
        CompanyId = companyId;
        CompanyName = companyName;
        ConglomerateId = conglomerateId;
        IsOverridden = true;
    }

    public event Action? OnContextChanged;
    public void NotifyContextChanged() => OnContextChanged?.Invoke();

    public void PopulateFromClaims(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return;

        // Always read user-identity fields (these don't change with store switch)
        AppUserId = user.FindFirstValue("app_user_id");
        if (Enum.TryParse<StoreRole>(user.FindFirstValue("store_role"), out var role))
            Role = role;

        DisplayName = user.FindFirstValue("name") ?? user.FindFirstValue("preferred_username");
        Email = user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email);
        ExternalSubjectId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        PreferredUsername = user.FindFirstValue("preferred_username");

        // Only set store/company from claims if not manually overridden
        if (!IsOverridden)
        {
            StoreId = user.FindFirstValue("store_id");
            CompanyId = user.FindFirstValue("company_id");
            ConglomerateId = user.FindFirstValue("conglomerate_id");
            StoreName = user.FindFirstValue("store_name");
            CompanyName = user.FindFirstValue("company_name");
        }
    }
}
