using System.Globalization;
using BikePOS.Data;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Services;

public class ShopCultureService
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private CultureInfo? _cached;

    public ShopCultureService(IDbContextFactory<BikePosContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<CultureInfo> GetCultureAsync()
    {
        if (_cached is not null) return _cached;

        using var context = _dbFactory.CreateDbContext();
        var setting = await context.ShopSetting.FirstOrDefaultAsync(s => s.Key == "shop_locale");
        var locale = setting?.Value ?? "es-CR";

        try { _cached = new CultureInfo(locale); }
        catch { _cached = new CultureInfo("es-CR"); }

        return _cached;
    }

    public void Invalidate() => _cached = null;
}
