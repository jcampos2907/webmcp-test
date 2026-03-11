using Microsoft.EntityFrameworkCore;
using BikePOS.Models;

namespace BikePOS.Data;

public class BikePosContext(DbContextOptions<BikePosContext> options) : DbContext(options)
{
    public DbSet<Bike> Bike { get; set; } = default!;
    public DbSet<ServiceTicket> ServiceTicket { get; set; } = default!;
    public DbSet<Mechanic> Mechanic { get; set; } = default!;
    public DbSet<Service> Service { get; set; } = default!;
    public DbSet<Product> Product { get; set; } = default!;
    public DbSet<TicketProduct> TicketProduct { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceTicket>(entity =>
        {
            entity.HasOne(t => t.Bike)
                .WithMany()
                .HasForeignKey(t => t.BikeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Mechanic)
                .WithMany()
                .HasForeignKey(t => t.MechanicId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.BaseService)
                .WithMany()
                .HasForeignKey(t => t.BaseServiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TicketProduct>(entity =>
        {
            entity.HasOne(tp => tp.ServiceTicket)
                .WithMany(t => t.TicketProducts)
                .HasForeignKey(tp => tp.ServiceTicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tp => tp.Product)
                .WithMany()
                .HasForeignKey(tp => tp.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(tp => new { tp.ServiceTicketId, tp.ProductId });
        });
    }
}
