using BikePOS.Data;
using BikePOS.Models;
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Commands;

public record CreateServiceRequest(string Name, string? Description, decimal DefaultPrice, int? EstimatedMinutes, string? StoreId);
public record CreateServiceResult(string Id);

public record UpdateServiceRequest(string Id, string Name, string? Description, decimal DefaultPrice, int? EstimatedMinutes);
public record DeleteServiceRequest(string Id);

public class CreateServiceCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly PermissionGuard _guard;

    public CreateServiceCommandHandler(IDbContextFactory<BikePosContext> dbFactory, PermissionGuard guard)
    {
        _dbFactory = dbFactory;
        _guard = guard;
    }

    public async Task<CreateServiceResult> HandleAsync(CreateServiceRequest request, CancellationToken ct = default)
    {
        _guard.Require("services.manage");
        using var db = _dbFactory.CreateDbContext();
        var service = new Service
        {
            Name = request.Name,
            Description = request.Description,
            DefaultPrice = request.DefaultPrice,
            EstimatedMinutes = request.EstimatedMinutes,
            StoreId = request.StoreId
        };
        db.Service.Add(service);
        await db.SaveChangesAsync(ct);
        return new CreateServiceResult(service.Id);
    }
}

public class UpdateServiceCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly PermissionGuard _guard;

    public UpdateServiceCommandHandler(IDbContextFactory<BikePosContext> dbFactory, PermissionGuard guard)
    {
        _dbFactory = dbFactory;
        _guard = guard;
    }

    public async Task<bool> HandleAsync(UpdateServiceRequest request, CancellationToken ct = default)
    {
        _guard.Require("services.manage");
        using var db = _dbFactory.CreateDbContext();
        var service = await db.Service.FindAsync(new object[] { request.Id }, ct);
        if (service is null) return false;

        service.Name = request.Name;
        service.Description = request.Description;
        service.DefaultPrice = request.DefaultPrice;
        service.EstimatedMinutes = request.EstimatedMinutes;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public class DeleteServiceCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly PermissionGuard _guard;

    public DeleteServiceCommandHandler(IDbContextFactory<BikePosContext> dbFactory, PermissionGuard guard)
    {
        _dbFactory = dbFactory;
        _guard = guard;
    }

    public async Task<bool> HandleAsync(DeleteServiceRequest request, CancellationToken ct = default)
    {
        _guard.Require("services.manage");
        using var db = _dbFactory.CreateDbContext();
        var service = await db.Service.FindAsync(new object[] { request.Id }, ct);
        if (service is null) return false;

        db.Service.Remove(service);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
