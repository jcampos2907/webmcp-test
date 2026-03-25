using BikePOS.Data;
using BikePOS.Interfaces.Repositories;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Infrastructure.Persistence;

public class MechanicRepository : IMechanicRepository
{
    private readonly BikePosContext _db;

    public MechanicRepository(BikePosContext db)
    {
        _db = db;
    }

    public async Task<Mechanic?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _db.Mechanic.FindAsync(new object[] { id }, ct);
    }

    public async Task<List<Mechanic>> GetActiveAsync(CancellationToken ct = default)
    {
        return await _db.Mechanic
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);
    }

    public async Task<List<Mechanic>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Mechanic.OrderBy(m => m.Name).ToListAsync(ct);
    }

    public async Task AddAsync(Mechanic mechanic, CancellationToken ct = default)
    {
        _db.Mechanic.Add(mechanic);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Mechanic mechanic, CancellationToken ct = default)
    {
        _db.Mechanic.Update(mechanic);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var mechanic = await _db.Mechanic.FindAsync(new object[] { id }, ct);
        if (mechanic != null)
            _db.Mechanic.Remove(mechanic);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
