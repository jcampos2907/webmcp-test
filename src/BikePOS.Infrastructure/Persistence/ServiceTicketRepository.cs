using BikePOS.Data;
using BikePOS.Interfaces.Repositories;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Infrastructure.Persistence;

public class ServiceTicketRepository : IServiceTicketRepository
{
    private readonly BikePosContext _db;

    public ServiceTicketRepository(BikePosContext db)
    {
        _db = db;
    }

    public async Task<ServiceTicket?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _db.ServiceTicket.FindAsync(new object[] { id }, ct);
    }

    public async Task<ServiceTicket?> GetByIdWithDetailsAsync(string id, CancellationToken ct = default)
    {
        return await _db.ServiceTicket
            .Include(t => t.Component)
            .Include(t => t.Customer)
            .Include(t => t.Mechanic)
            .Include(t => t.BaseService)
            .Include(t => t.TicketProducts).ThenInclude(tp => tp.Product)
            .Include(t => t.Charges)
            .Include(t => t.Events)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<List<ServiceTicket>> GetByStatusAsync(
        Domain.Aggregates.ServiceTicket.TicketStatus status, CancellationToken ct = default)
    {
        // Map domain enum to persistence enum
        var persistenceStatus = (Models.TicketStatus)(int)status;
        return await _db.ServiceTicket
            .Include(t => t.Component)
            .Include(t => t.Mechanic)
            .Where(t => t.Status == persistenceStatus)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<ServiceTicket>> GetOpenTicketsAsync(CancellationToken ct = default)
    {
        var openStatuses = new[] { Models.TicketStatus.Open, Models.TicketStatus.InProgress, Models.TicketStatus.WaitingForParts };
        return await _db.ServiceTicket
            .Include(t => t.Component)
            .Include(t => t.Mechanic)
            .Where(t => openStatuses.Contains(t.Status))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<ServiceTicket>> GetByMechanicAsync(string mechanicId, CancellationToken ct = default)
    {
        return await _db.ServiceTicket
            .Include(t => t.Component)
            .Include(t => t.Customer)
            .Where(t => t.MechanicId == mechanicId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetNextTicketNumberAsync(string? storeId, CancellationToken ct = default)
    {
        var max = await _db.ServiceTicket
            .Where(t => t.StoreId == storeId)
            .MaxAsync(t => (int?)t.TicketNumber, ct) ?? 0;
        return max + 1;
    }

    public async Task AddAsync(ServiceTicket ticket, CancellationToken ct = default)
    {
        _db.ServiceTicket.Add(ticket);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(ServiceTicket ticket, CancellationToken ct = default)
    {
        _db.ServiceTicket.Update(ticket);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
