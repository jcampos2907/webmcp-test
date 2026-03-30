using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Queries;

public class GetProductByIdQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public GetProductByIdQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<Product?> HandleAsync(string id, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Product.FindAsync(new object[] { id }, ct);
    }
}

public class ListProductsQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public ListProductsQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<Product>> HandleAsync(string? search = null, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.Product.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term)
                || (p.Sku != null && p.Sku.ToLower().Contains(term))
                || (p.Category != null && p.Category.ToLower().Contains(term)));
        }
        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }
}
