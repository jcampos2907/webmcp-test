using BikePOS.Data;
using BikePOS.Domain.Aggregates.Customer;
using BikePOS.Domain.Aggregates.Customer.Events;
using BikePOS.Interfaces.Events;
using BikePOS.Domain.Events;
using BikePOS.Infrastructure.Erp;
using BikePOS.Models;
using Microsoft.EntityFrameworkCore;

namespace BikePOS.Application.Commands;

public record CreateCustomerRequest(
    string FirstName, string LastName, string? Phone, string? Email,
    string? Street, string? City, string? State, string? ZipCode, string? Country,
    string? StoreId);
public record CreateCustomerResult(string Id);

public record UpdateCustomerRequest(
    string Id, string FirstName, string LastName, string? Phone, string? Email,
    string? Street, string? City, string? State, string? ZipCode, string? Country);

public record DeleteCustomerRequest(string Id);

public class CreateCustomerCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly SyncTriggerService _syncTrigger;

    public CreateCustomerCommandHandler(
        IDbContextFactory<BikePosContext> dbFactory,
        IDomainEventDispatcher eventDispatcher,
        SyncTriggerService syncTrigger)
    {
        _dbFactory = dbFactory;
        _eventDispatcher = eventDispatcher;
        _syncTrigger = syncTrigger;
    }

    public async Task<CreateCustomerResult> HandleAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Email = request.Email,
            Street = request.Street,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            StoreId = request.StoreId
        };
        db.Customer.Add(customer);
        await db.SaveChangesAsync(ct);

        // Domain event
        var domainEvent = new CustomerCreatedEvent(customer.Id, customer.FirstName, customer.LastName, request.StoreId);
        await _eventDispatcher.DispatchAsync(new IDomainEvent[] { domainEvent }, ct);

        _syncTrigger.TriggerPush("Customer", customer.Id);

        return new CreateCustomerResult(customer.Id);
    }
}

public class UpdateCustomerCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public UpdateCustomerCommandHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<bool> HandleAsync(UpdateCustomerRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var customer = await db.Customer.FindAsync(new object[] { request.Id }, ct);
        if (customer is null) return false;

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Street = request.Street;
        customer.City = request.City;
        customer.State = request.State;
        customer.ZipCode = request.ZipCode;
        customer.Country = request.Country;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public class DeleteCustomerCommandHandler
{
    private readonly IDbContextFactory<BikePosContext> _dbFactory;

    public DeleteCustomerCommandHandler(IDbContextFactory<BikePosContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<bool> HandleAsync(DeleteCustomerRequest request, CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var customer = await db.Customer.FindAsync(new object[] { request.Id }, ct);
        if (customer is null) return false;

        db.Customer.Remove(customer);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
