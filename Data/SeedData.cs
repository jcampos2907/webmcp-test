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

        var storeId = context.Store.First().Id;

        // Seed payment terminals (independent guard)
        if (!context.PaymentTerminal.Any())
        {
            context.PaymentTerminal.AddRange(
                new PaymentTerminal
                {
                    StoreId = storeId,
                    Name = "Front Counter",
                    IpAddress = "192.168.1.100",
                    Port = 8080,
                    Provider = TerminalProvider.Ingenico,
                    IsActive = true
                },
                new PaymentTerminal
                {
                    StoreId = storeId,
                    Name = "Service Desk",
                    IpAddress = "192.168.1.101",
                    Port = 8080,
                    Provider = TerminalProvider.Ingenico,
                    IsActive = true
                });
            context.SaveChanges();
        }

        // Seed test tickets for POS testing (independent guard)
        if (!context.ServiceTicket.Any() && context.Component.Any() && context.Mechanic.Any())
        {
            var mechanics = context.Mechanic.ToList();
            var services = context.Service.ToList();
            var components = context.Component.Include(c => c.Customer).ToList();

            // Ticket 1: Completed, ready to charge (full amount)
            var ticket1 = new ServiceTicket
            {
                ComponentId = components[0].Id,
                CustomerId = components[0].CustomerId,
                MechanicId = mechanics[0].Id,
                BaseServiceId = services[0].Id,
                Price = services[0].DefaultPrice,
                Status = TicketStatus.Completed,
                Description = "Full tune-up on Tarmac SL7",
                StoreId = storeId,
                TicketNumber = 1,
                CreatedBy = "seed"
            };

            // Ticket 2: Completed, will have a partial payment already
            var ticket2 = new ServiceTicket
            {
                ComponentId = components[1].Id,
                CustomerId = components[1].CustomerId,
                MechanicId = mechanics[1].Id,
                BaseServiceId = services.Count > 3 ? services[3].Id : services[0].Id,
                Price = services.Count > 3 ? services[3].DefaultPrice : 250m,
                Status = TicketStatus.Completed,
                Description = "Full overhaul on The Road Warrior",
                StoreId = storeId,
                TicketNumber = 2,
                CreatedBy = "seed"
            };

            // Ticket 3: In progress
            var ticket3 = new ServiceTicket
            {
                ComponentId = components[2].Id,
                CustomerId = components[2].CustomerId,
                MechanicId = mechanics.Count > 2 ? mechanics[2].Id : mechanics[0].Id,
                BaseServiceId = services.Count > 4 ? services[4].Id : services[0].Id,
                Price = services.Count > 4 ? services[4].DefaultPrice : 15m,
                Status = TicketStatus.InProgress,
                Description = "Flat repair on Allez Sprint",
                StoreId = storeId,
                TicketNumber = 3,
                CreatedBy = "seed"
            };

            context.ServiceTicket.AddRange(ticket1, ticket2, ticket3);
            context.SaveChanges();

            // Add a partial payment on ticket 2 ($100 deposit on $250 overhaul)
            context.Charge.Add(new Charge
            {
                ServiceTicketId = ticket2.Id,
                Amount = 100m,
                ChargedAt = DateTime.UtcNow.AddHours(-2),
                CashierName = "seed",
                PaymentMethod = PaymentMethod.Cash,
                PaymentStatus = PaymentStatus.Completed,
                CompletedAt = DateTime.UtcNow.AddHours(-2),
                StoreId = storeId,
                CreatedBy = "seed"
            });
            context.SaveChanges();
        }

        if (context.Component.Any())
        {
            return;
        }

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
