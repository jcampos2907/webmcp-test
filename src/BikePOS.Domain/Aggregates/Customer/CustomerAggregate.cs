using BikePOS.Domain.Common;
using BikePOS.Domain.Aggregates.Customer.Events;
using BikePOS.Domain.ValueObjects;

namespace BikePOS.Domain.Aggregates.Customer;

/// <summary>
/// Customer aggregate root. Manages customer identity, contact info,
/// address, and owned components (bikes, rims, etc.).
/// </summary>
public class CustomerAggregate : AggregateRoot
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Street { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? ZipCode { get; private set; }
    public string? Country { get; private set; }
    public string? StoreId { get; private set; }

    private readonly List<OwnedComponent> _components = new();
    public IReadOnlyList<OwnedComponent> Components => _components.AsReadOnly();

    private CustomerAggregate() { }

    public static CustomerAggregate Create(
        string firstName,
        string lastName,
        string? phone,
        string? email,
        string? storeId)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        // Validate value objects if provided
        if (!string.IsNullOrWhiteSpace(email))
            ValueObjects.Email.Create(email); // throws if invalid
        if (!string.IsNullOrWhiteSpace(phone))
            ValueObjects.PhoneNumber.Create(phone); // throws if invalid

        var customer = new CustomerAggregate
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            StoreId = storeId
        };

        customer.AddDomainEvent(new CustomerCreatedEvent(customer.Id, firstName, lastName, storeId));
        return customer;
    }

    public static CustomerAggregate Reconstitute(
        string id,
        string firstName,
        string lastName,
        string? phone,
        string? email,
        string? street,
        string? city,
        string? state,
        string? zipCode,
        string? country,
        string? storeId,
        IEnumerable<OwnedComponent> components)
    {
        var customer = new CustomerAggregate
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email,
            Street = street,
            City = city,
            State = state,
            ZipCode = zipCode,
            Country = country,
            StoreId = storeId
        };
        customer.Id = id;
        customer._components.AddRange(components);
        return customer;
    }

    // ── Contact info ────────────────────────────────────────────────

    public void UpdateContactInfo(string firstName, string lastName, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        if (!string.IsNullOrWhiteSpace(email))
            ValueObjects.Email.Create(email);
        if (!string.IsNullOrWhiteSpace(phone))
            ValueObjects.PhoneNumber.Create(phone);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
    }

    // ── Address ─────────────────────────────────────────────────────

    public void UpdateAddress(string? street, string? city, string? state, string? zipCode, string? country)
    {
        Street = street?.Trim();
        City = city?.Trim();
        State = state?.Trim();
        ZipCode = zipCode?.Trim();
        Country = country?.Trim();
    }

    // ── Components ──────────────────────────────────────────────────

    public void AddComponent(string componentId, string componentType, string? name, string brand)
    {
        if (_components.Any(c => c.ComponentId == componentId))
            throw new InvalidOperationException($"Component {componentId} already belongs to this customer.");

        _components.Add(new OwnedComponent(componentId, componentType, name, brand));
        AddDomainEvent(new ComponentAddedToCustomerEvent(Id, componentId, componentType));
    }

    public void RemoveComponent(string componentId)
    {
        var component = _components.FirstOrDefault(c => c.ComponentId == componentId);
        if (component == null)
            throw new InvalidOperationException($"Component {componentId} not found for this customer.");

        _components.Remove(component);
    }
}

/// <summary>Read-only snapshot of a component owned by the customer.</summary>
public class OwnedComponent
{
    public string ComponentId { get; }
    public string ComponentType { get; }
    public string? Name { get; }
    public string Brand { get; }

    public OwnedComponent(string componentId, string componentType, string? name, string brand)
    {
        ComponentId = componentId;
        ComponentType = componentType;
        Name = name;
        Brand = brand;
    }
}
