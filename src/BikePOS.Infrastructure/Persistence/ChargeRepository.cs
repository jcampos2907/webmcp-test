using BikePOS.Data;
using BikePOS.Interfaces.Repositories;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Infrastructure.Persistence;

public class ChargeRepository : IChargeRepository
{
    private readonly BikePosContext _db;

    public ChargeRepository(BikePosContext db)
    {
        _db = db;
    }

    public async Task<Charge?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _db.Charge.FindAsync(new object[] { id }, ct);
    }

    public async Task<List<Charge>> GetByTicketIdAsync(string ticketId, CancellationToken ct = default)
    {
        return await _db.Charge
            .Where(c => c.ServiceTicketId == ticketId)
            .OrderByDescending(c => c.ChargedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Charge>> GetRecentAsync(int count = 20, CancellationToken ct = default)
    {
        return await _db.Charge
            .Include(c => c.ServiceTicket)
            .OrderByDescending(c => c.ChargedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Charge charge, CancellationToken ct = default)
    {
        _db.Charge.Add(charge);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Charge charge, CancellationToken ct = default)
    {
        _db.Charge.Update(charge);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
