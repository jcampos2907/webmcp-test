using BikePOS.Data;
using BikePOS.Interfaces.Repositories;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Infrastructure.Persistence;

public class ProductRepository : IProductRepository
{
    private readonly BikePosContext _db;

    public ProductRepository(BikePosContext db)
    {
        _db = db;
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _db.Product.FindAsync(new object[] { id }, ct);
    }

    public async Task<List<Product>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Product.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<List<Product>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = _db.Product.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(p =>
                p.Name.Contains(query) ||
                (p.Sku != null && p.Sku.Contains(query)) ||
                (p.Category != null && p.Category.Contains(query)));
        }
        return await q.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        _db.Product.Add(product);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _db.Product.Update(product);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var product = await _db.Product.FindAsync(new object[] { id }, ct);
        if (product != null)
            _db.Product.Remove(product);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
