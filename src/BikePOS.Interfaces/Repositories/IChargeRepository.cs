namespace BikePOS.Interfaces.Repositories;

public interface IChargeRepository
{
    Task<Models.Charge?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Models.Charge>> GetByTicketIdAsync(string ticketId, CancellationToken ct = default);
    Task<List<Models.Charge>> GetRecentAsync(int count = 20, CancellationToken ct = default);
    Task AddAsync(Models.Charge charge, CancellationToken ct = default);
    Task UpdateAsync(Models.Charge charge, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
