namespace BikePOS.Interfaces.Repositories;

public interface IMechanicRepository
{
    Task<Models.Mechanic?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Models.Mechanic>> GetActiveAsync(CancellationToken ct = default);
    Task<List<Models.Mechanic>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Models.Mechanic mechanic, CancellationToken ct = default);
    Task UpdateAsync(Models.Mechanic mechanic, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
