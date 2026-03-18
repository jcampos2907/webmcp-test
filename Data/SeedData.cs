using Microsoft.EntityFrameworkCore;
using BikePOS.Models;

namespace BikePOS.Data;

public class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var context = new BikePosContext(
            serviceProvider.GetRequiredService<
                DbContextOptions<BikePosContext>>());

        if (context == null || context.Component == null)
        {
            throw new NullReferenceException(
                "Null BikePosContext or Component DbSet");
        }

        // Seed tenant hierarchy if none exists
        if (!context.Conglomerate.Any())
        {
            var conglomerate = new Conglomerate { Name = "Default Group" };
            context.Conglomerate.Add(conglomerate);
            context.SaveChanges();

            var company = new Company
            {
                ConglomerateId = conglomerate.Id,
                Name = "BikePOS Default",
                Locale = "es-CR",
                Currency = "CRC"
            };
            context.Company.Add(company);
            context.SaveChanges();

            var store = new Store
            {
                CompanyId = company.Id,
                Name = "Main Store",
                Address = "San José, Costa Rica"
            };
            context.Store.Add(store);
            context.SaveChanges();
        }

        if (context.Component.Any())
        {
            return;
        }

        var storeId = context.Store.First().Id;

        // Component types setting
        context.ShopSetting.Add(new ShopSetting { Key = "component_types", Value = "Bicicleta, Aro, Pedal, Marco, Rueda, Otro", StoreId = storeId });
        context.SaveChanges();

        // Meta field definitions
        var taxIdField = new MetaFieldDefinition { Key = "tax_id", Label = "RUT / Tax ID", FieldType = "text", IsRequired = false, SortOrder = 1, StoreId = storeId };
        var companyField = new MetaFieldDefinition { Key = "company_name", Label = "Empresa", FieldType = "text", IsRequired = false, SortOrder = 2, StoreId = storeId };
        context.MetaFieldDefinition.AddRange(taxIdField, companyField);
        context.SaveChanges();

        // Customers
        var juan = new Customer { FirstName = "Juan Ignacio", LastName = "Campos", Phone = "555-1001", Email = "juan@example.com", City = "Santiago", Country = "Chile", StoreId = storeId };
        var mario = new Customer { FirstName = "Mario", LastName = "Barahona", Phone = "555-1002", Email = "mario@example.com", City = "Santiago", Country = "Chile", StoreId = storeId };
        var laura = new Customer { FirstName = "Laura", LastName = "Mendez", Phone = "555-1003", Email = "laura@example.com", City = "Valparaiso", Country = "Chile", StoreId = storeId };
        context.Customer.AddRange(juan, mario, laura);
        context.SaveChanges();

        // Sample meta values
        context.CustomerMetaValue.Add(new CustomerMetaValue { CustomerId = juan.Id, MetaFieldDefinitionId = taxIdField.Id, Value = "12.345.678-9" });
        context.SaveChanges();

        // Components (serviceable items)
        context.Component.AddRange(
            new Component { Name = "Tarmac SL7", Sku = "MM1979", Color = "Red", Brand = "Specialized", ComponentType = "Bicicleta", Price = 16000, CustomerId = juan.Id, StoreId = storeId },
            new Component { Name = "The Road Warrior", Sku = "TRW1981", Color = "Blue", Brand = "Trek", ComponentType = "Bicicleta", Price = 18000, CustomerId = mario.Id, StoreId = storeId },
            new Component { Name = "Allez Sprint", Sku = "MMBT1985", Color = "Yellow", Brand = "Specialized", ComponentType = "Bicicleta", Price = 3550, CustomerId = juan.Id, StoreId = storeId },
            new Component { Name = "Domane SLR", Sku = "MMFR2015", Color = "Black", Brand = "Trek", ComponentType = "Bicicleta", Price = 8430, CustomerId = laura.Id, StoreId = storeId },
            new Component { Name = "Venge Pro", Sku = "FAMMS2024", Color = "White", Brand = "Specialized", ComponentType = "Bicicleta", Price = 8430, CustomerId = mario.Id, StoreId = storeId });

        context.Mechanic.AddRange(
            new Mechanic { Name = "Mike Rodriguez", Phone = "555-0101", Email = "mike@bikepos.local", StoreId = storeId },
            new Mechanic { Name = "Sarah Chen", Phone = "555-0102", Email = "sarah@bikepos.local", StoreId = storeId },
            new Mechanic { Name = "James Park", Phone = "555-0103", Email = "james@bikepos.local", StoreId = storeId });

        context.Service.AddRange(
            new Service { Name = "Tune-Up Sencillo", Description = "Full bike tune-up including derailleur adjustment, brake check, and lubrication", DefaultPrice = 75.00m, EstimatedMinutes = 60, StoreId = storeId },
            new Service { Name = "Brake Replacement", Description = "Replace brake pads and adjust calipers", DefaultPrice = 45.00m, EstimatedMinutes = 30, StoreId = storeId },
            new Service { Name = "Wheel Truing", Description = "True wheel and check spoke tension", DefaultPrice = 35.00m, EstimatedMinutes = 25, StoreId = storeId },
            new Service { Name = "Full Overhaul", Description = "Complete disassembly, cleaning, inspection, and reassembly", DefaultPrice = 250.00m, EstimatedMinutes = 180, StoreId = storeId },
            new Service { Name = "Flat Repair", Description = "Remove tire, patch or replace tube, reinstall", DefaultPrice = 15.00m, EstimatedMinutes = 15, StoreId = storeId });

        context.Product.AddRange(
            new Product { Name = "Fibras AMP", Sku = "BP001", Price = 20000m, QuantityInStock = 50, Category = "Brakes", StoreId = storeId },
            new Product { Name = "Inner Tube 700c", Sku = "IT700", Price = 7.99m, QuantityInStock = 100, Category = "Tires & Tubes", StoreId = storeId },
            new Product { Name = "Chain 11-Speed", Sku = "CH11S", Price = 34.99m, QuantityInStock = 25, Category = "Drivetrain", StoreId = storeId },
            new Product { Name = "Tire 700x25c", Sku = "TR725", Price = 44.99m, QuantityInStock = 30, Category = "Tires & Tubes", StoreId = storeId },
            new Product { Name = "Cable Set (Brake + Shift)", Sku = "CBS01", Price = 19.99m, QuantityInStock = 40, Category = "Cables", StoreId = storeId },
            new Product { Name = "Chain Lube 4oz", Sku = "CL004", Price = 9.99m, QuantityInStock = 60, Category = "Maintenance", StoreId = storeId });

        context.SaveChanges();
    }
}
