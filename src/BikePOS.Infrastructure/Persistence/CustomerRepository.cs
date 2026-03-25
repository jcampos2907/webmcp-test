using BikePOS.Data;
using BikePOS.Interfaces.Repositories;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Infrastructure.Persistence;

public class CustomerRepository : ICustomerRepository
{
    private readonly BikePosContext _db;

    public CustomerRepository(BikePosContext db)
    {
        _db = db;
    }

    public async Task<Customer?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _db.Customer.FindAsync(new object[] { id }, ct);
    }

    public async Task<Customer?> GetByIdWithComponentsAsync(string id, CancellationToken ct = default)
    {
        return await _db.Customer
            .Include(c => c.Components)
            .Include(c => c.MetaValues).ThenInclude(mv => mv.MetaFieldDefinition)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Customer>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = _db.Customer.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(c =>
                c.FirstName.Contains(query) ||
                c.LastName.Contains(query) ||
                (c.Phone != null && c.Phone.Contains(query)) ||
                (c.Email != null && c.Email.Contains(query)));
        }
        return await q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(ct);
    }

    public async Task<List<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Customer
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        _db.Customer.Add(customer);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _db.Customer.Update(customer);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var customer = await _db.Customer.FindAsync(new object[] { id }, ct);
        if (customer != null)
            _db.Customer.Remove(customer);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
