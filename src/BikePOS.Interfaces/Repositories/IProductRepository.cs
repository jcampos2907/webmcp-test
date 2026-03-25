namespace BikePOS.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Models.Product?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Models.Product>> GetAllAsync(CancellationToken ct = default);
    Task<List<Models.Product>> SearchAsync(string? query, CancellationToken ct = default);
    Task AddAsync(Models.Product product, CancellationToken ct = default);
    Task UpdateAsync(Models.Product product, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
