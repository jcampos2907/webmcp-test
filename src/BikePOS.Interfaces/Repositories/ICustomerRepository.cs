namespace BikePOS.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Models.Customer?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Models.Customer?> GetByIdWithComponentsAsync(string id, CancellationToken ct = default);
    Task<List<Models.Customer>> SearchAsync(string? query, CancellationToken ct = default);
    Task<List<Models.Customer>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Models.Customer customer, CancellationToken ct = default);
    Task UpdateAsync(Models.Customer customer, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
