using BikePOS.Data;
using BikePOS.Models;
using BikePOS.Services;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Commands;

public record CreateProductRequest(string Name, string? Sku, decimal Price, int QuantityInStock, string? Category, string? StoreId);
public record CreateProductResult(string Id);

public record UpdateProductRequest(string Id, string Name, string? Sku, decimal Price, int QuantityInStock, string? Category);
public record DeleteProductRequest(string Id);

public class CreateProductCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly PermissionGuard _guard;

    public CreateProductCommandHandler(IDbContextFactory<BikePosContext> dbFactory, PermissionGuard guard)
    {
        _dbFactory = dbFactory;
        _guard = guard;
    }

    public async Task<CreateProductResult> HandleAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        _guard.Require("products.manage");
        using var db = _dbFactory.CreateDbContext();
        var product = new Product
        {
            Name = request.Name,
            Sku = request.Sku,
            Price = request.Price,
            QuantityInStock = request.QuantityInStock,
            Category = request.Category,
            StoreId = request.StoreId
        };
        db.Product.Add(product);
        await db.SaveChangesAsync(ct);
        return new CreateProductResult(product.Id);
    }
}

public class UpdateProductCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly PermissionGuard _guard;

    public UpdateProductCommandHandler(IDbContextFactory<BikePosContext> dbFactory, PermissionGuard guard)
    {
        _dbFactory = dbFactory;
        _guard = guard;
    }

    public async Task<bool> HandleAsync(UpdateProductRequest request, CancellationToken ct = default)
    {
        _guard.Require("products.manage");
        using var db = _dbFactory.CreateDbContext();
        var product = await db.Product.FindAsync(new object[] { request.Id }, ct);
        if (product is null) return false;

        product.Name = request.Name;
        product.Sku = request.Sku;
        product.Price = request.Price;
        product.QuantityInStock = request.QuantityInStock;
        product.Category = request.Category;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public class DeleteProductCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly PermissionGuard _guard;

    public DeleteProductCommandHandler(IDbContextFactory<BikePosContext> dbFactory, PermissionGuard guard)
    {
        _dbFactory = dbFactory;
        _guard = guard;
    }

    public async Task<bool> HandleAsync(DeleteProductRequest request, CancellationToken ct = default)
    {
        _guard.Require("products.manage");
        using var db = _dbFactory.CreateDbContext();
        var product = await db.Product.FindAsync(new object[] { request.Id }, ct);
        if (product is null) return false;

        db.Product.Remove(product);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
