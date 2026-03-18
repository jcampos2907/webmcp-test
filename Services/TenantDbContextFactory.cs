using BikePOS.Data;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Services;

/// <summary>
/// Wraps the real IDbContextFactory to automatically set CurrentStoreId
/// from TenantContext on every created context. Registered as the primary
/// IDbContextFactory so all injection points get tenant-scoped contexts.
/// </summary>
public class TenantDbContextFactory : IDbContextFactory<BikePosContext>
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
