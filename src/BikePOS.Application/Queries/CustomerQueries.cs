using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Queries;

public record CustomerListDto(string Id, string FirstName, string LastName, string FullName, string? Phone, string? Email, string? City);

public class GetCustomerByIdQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public GetCustomerByIdQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<Customer?> HandleAsync(string id, bool includeComponents = false, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.Customer.AsQueryable();
        if (includeComponents)
            query = query.Include(c => c.Components);
        return await query.FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}

public class ListCustomersQueryHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public ListCustomersQueryHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<Customer>> HandleAsync(string? search = null, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.Customer.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)));
        }
        return await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(ct);
    }
}
