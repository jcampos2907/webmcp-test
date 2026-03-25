using BikePOS.Data;
using BikePOS.Interfaces.Repositories;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Infrastructure.Persistence;

public class ComponentRepository : IComponentRepository
{
    private readonly BikePosContext _db;

    public ComponentRepository(BikePosContext db)
    {
        _db = db;
    }

    public async Task<Component?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _db.Component.FindAsync(new object[] { id }, ct);
    }

    public async Task<List<Component>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default)
    {
        return await _db.Component
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<List<Component>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = _db.Component.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(c =>
                (c.Name != null && c.Name.Contains(query)) ||
                c.Brand.Contains(query) ||
                c.Color.Contains(query) ||
                c.Sku.Contains(query));
        }
        return await q.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<List<Component>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Component.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task AddAsync(Component component, CancellationToken ct = default)
    {
        _db.Component.Add(component);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Component component, CancellationToken ct = default)
    {
        _db.Component.Update(component);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var component = await _db.Component.FindAsync(new object[] { id }, ct);
        if (component != null)
            _db.Component.Remove(component);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
