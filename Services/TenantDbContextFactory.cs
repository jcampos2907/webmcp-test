using BikePOS.Data;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Services;

/// <summary>
/// Wraps IDbContextFactory to automatically set CurrentStoreId on created contexts.
/// Inject this instead of IDbContextFactory for tenant-scoped data access.
/// </summary>
public class TenantDbContextFactory
{
    private readonly IDbContextFactory<BikePosContext> _inner;
    private readonly TenantContext _tenant;

    public TenantDbContextFactory(IDbContextFactory<BikePosContext> inner, TenantContext tenant)
    {
        _inner = inner;
        _tenant = tenant;
    }

    public BikePosContext CreateDbContext()
    {
        var context = _inner.CreateDbContext();
        context.CurrentStoreId = _tenant.StoreId;
        return context;
    }
}
