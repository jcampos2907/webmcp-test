using Microsoft.EntityFrameworkCore;
using webmcp.Models;

namespace webmcp.Data;

public class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var context = new webmcpContext(
            serviceProvider.GetRequiredService<
                DbContextOptions<webmcpContext>>());

        if (context == null || context.Bike == null)
        {
            throw new NullReferenceException(
                "Null webmcpContext or Bike DbSet");
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

        context.SaveChanges();
    }
}