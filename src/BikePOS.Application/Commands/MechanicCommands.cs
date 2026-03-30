using BikePOS.Data;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Commands;

public record CreateMechanicRequest(string Name, string? Phone, string? Email, bool IsActive, string? StoreId);
public record CreateMechanicResult(string Id);

public record UpdateMechanicRequest(string Id, string Name, string? Phone, string? Email, bool IsActive);
public record DeleteMechanicRequest(string Id);

public class CreateMechanicCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public CreateMechanicCommandHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<CreateMechanicResult> HandleAsync(CreateMechanicRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var mechanic = new Mechanic
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = request.IsActive,
            StoreId = request.StoreId
        };
        db.Mechanic.Add(mechanic);
        await db.SaveChangesAsync(ct);
        return new CreateMechanicResult(mechanic.Id);
    }
}

public class UpdateMechanicCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public UpdateMechanicCommandHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<bool> HandleAsync(UpdateMechanicRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var mechanic = await db.Mechanic.FindAsync(new object[] { request.Id }, ct);
        if (mechanic is null) return false;

        mechanic.Name = request.Name;
        mechanic.Phone = request.Phone;
        mechanic.Email = request.Email;
        mechanic.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public class DeleteMechanicCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public DeleteMechanicCommandHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<bool> HandleAsync(DeleteMechanicRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var mechanic = await db.Mechanic.FindAsync(new object[] { request.Id }, ct);
        if (mechanic is null) return false;

        db.Mechanic.Remove(mechanic);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
