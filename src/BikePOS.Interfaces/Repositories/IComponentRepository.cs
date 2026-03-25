namespace BikePOS.Interfaces.Repositories;

public interface IComponentRepository
{
    Task<Models.Component?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Models.Component>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default);
    Task<List<Models.Component>> SearchAsync(string? query, CancellationToken ct = default);
    Task<List<Models.Component>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Models.Component component, CancellationToken ct = default);
    Task UpdateAsync(Models.Component component, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
