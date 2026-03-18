using System.Globalization;
using BikePOS.Data;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Services;

public class ShopCultureService
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly TenantContext _tenant;
    private CultureInfo? _cached;

    public ShopCultureService(IDbContextFactory<BikePosContext> dbFactory, TenantContext tenant)
    {
        _dbFactory = dbFactory;
        _tenant = tenant;
    }

    public async Task<CultureInfo> GetCultureAsync()
    {
        if (_cached is not null) return _cached;

        using var context = _dbFactory.CreateDbContext();

        // Try to read locale from the Company (tenant-level setting)
        if (_tenant.CompanyId.HasValue)
        {
            var company = await context.Company.FindAsync(_tenant.CompanyId.Value);
            if (company != null && !string.IsNullOrEmpty(company.Locale))
            {
                try { _cached = new CultureInfo(company.Locale); return _cached; }
                catch { /* fall through */ }
            }
        }

        // Fallback: read from ShopSetting (legacy) or default
        context.CurrentStoreId = _tenant.StoreId;
        var setting = await context.ShopSetting.FirstOrDefaultAsync(s => s.Key == "shop_locale");
        var locale = setting?.Value ?? "es-CR";

        try { _cached = new CultureInfo(locale); }
        catch { _cached = new CultureInfo("es-CR"); }

        return _cached;
    }

    public void Invalidate() => _cached = null;
}
