using BikePOS.Domain.Aggregates.ServiceTicket;

namespace BikePOS.Interfaces.Repositories;

public interface IServiceTicketRepository
{
    Task<Models.ServiceTicket?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Models.ServiceTicket?> GetByIdWithDetailsAsync(string id, CancellationToken ct = default);
    Task<List<Models.ServiceTicket>> GetByStatusAsync(TicketStatus status, CancellationToken ct = default);
    Task<List<Models.ServiceTicket>> GetOpenTicketsAsync(CancellationToken ct = default);
    Task<List<Models.ServiceTicket>> GetByMechanicAsync(string mechanicId, CancellationToken ct = default);
    Task<int> GetNextTicketNumberAsync(string? storeId, CancellationToken ct = default);
    Task AddAsync(Models.ServiceTicket ticket, CancellationToken ct = default);
    Task UpdateAsync(Models.ServiceTicket ticket, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
