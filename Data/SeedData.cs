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

        if (context == null || context.Bike == null)
        {
            throw new NullReferenceException(
                "Null BikePosContext or Bike DbSet");
        }

        if (context.Bike.Any())
        {
            return;
        }

        context.Bike.AddRange(
            new Bike
            {
                Name = "Batman's Batcycle",
                Sku = "MM1979",
                Color = "Red",
                Brand = "Specialized",
                Price = 16000,
            },
            new Bike
            {
                Name = "The Road Warrior",
                Sku = "TRW1981",
                Color = "Blue",
                Brand = "Trek",
                Price = 18000,
            },
            new Bike
            {
                Name = "Bike12",
                Sku = "MMBT1985",
                Color = "Yellow",
                Brand = "Specialized",
                Price = 3550,
            },
            new Bike
            {
                Name = "Bike13",
                Sku = "MMFR2015",
                Color = "Black",
                Brand = "Specialized",
                Price = 8430,
            },
            new Bike
            {
                Name = "Bike14",
                Sku = "FAMMS2024",
                Color = "White",
                Brand = "Trek",
                Price = 8430,
            });

        context.Mechanic.AddRange(
            new Mechanic { Name = "Mike Rodriguez", Phone = "555-0101", Email = "mike@bikepos.local" },
            new Mechanic { Name = "Sarah Chen", Phone = "555-0102", Email = "sarah@bikepos.local" },
            new Mechanic { Name = "James Park", Phone = "555-0103", Email = "james@bikepos.local" });

        context.Service.AddRange(
            new Service { Name = "Tune-Up", Description = "Full bike tune-up including derailleur adjustment, brake check, and lubrication", DefaultPrice = 75.00m, EstimatedMinutes = 60 },
            new Service { Name = "Brake Replacement", Description = "Replace brake pads and adjust calipers", DefaultPrice = 45.00m, EstimatedMinutes = 30 },
            new Service { Name = "Wheel Truing", Description = "True wheel and check spoke tension", DefaultPrice = 35.00m, EstimatedMinutes = 25 },
            new Service { Name = "Full Overhaul", Description = "Complete disassembly, cleaning, inspection, and reassembly", DefaultPrice = 250.00m, EstimatedMinutes = 180 },
            new Service { Name = "Flat Repair", Description = "Remove tire, patch or replace tube, reinstall", DefaultPrice = 15.00m, EstimatedMinutes = 15 });

        context.Product.AddRange(
            new Product { Name = "Brake Pads (Pair)", Sku = "BP001", Price = 12.99m, QuantityInStock = 50, Category = "Brakes" },
            new Product { Name = "Inner Tube 700c", Sku = "IT700", Price = 7.99m, QuantityInStock = 100, Category = "Tires & Tubes" },
            new Product { Name = "Chain 11-Speed", Sku = "CH11S", Price = 34.99m, QuantityInStock = 25, Category = "Drivetrain" },
            new Product { Name = "Tire 700x25c", Sku = "TR725", Price = 44.99m, QuantityInStock = 30, Category = "Tires & Tubes" },
            new Product { Name = "Cable Set (Brake + Shift)", Sku = "CBS01", Price = 19.99m, QuantityInStock = 40, Category = "Cables" },
            new Product { Name = "Chain Lube 4oz", Sku = "CL004", Price = 9.99m, QuantityInStock = 60, Category = "Maintenance" });

        context.SaveChanges();
    }
}
