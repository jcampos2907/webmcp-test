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
    public DbSet<Charge> Charge { get; set; } = default!;
    public DbSet<Customer> Customer { get; set; } = default!;
    public DbSet<MetaFieldDefinition> MetaFieldDefinition { get; set; } = default!;
    public DbSet<CustomerMetaValue> CustomerMetaValue { get; set; } = default!;
    public DbSet<ShopSetting> ShopSetting { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Bike>(entity =>
        {
            entity.HasOne(b => b.Customer)
                .WithMany(c => c.Bikes)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

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

        modelBuilder.Entity<Charge>(entity =>
        {
            entity.HasOne(c => c.ServiceTicket)
                .WithMany(t => t.Charges)
                .HasForeignKey(c => c.ServiceTicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerMetaValue>(entity =>
        {
            entity.HasOne(mv => mv.Customer)
                .WithMany(c => c.MetaValues)
                .HasForeignKey(mv => mv.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mv => mv.MetaFieldDefinition)
                .WithMany()
                .HasForeignKey(mv => mv.MetaFieldDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(mv => new { mv.CustomerId, mv.MetaFieldDefinitionId }).IsUnique();
        });

        modelBuilder.Entity<MetaFieldDefinition>(entity =>
        {
            entity.HasOne(f => f.ConditionalOnField)
                .WithMany()
                .HasForeignKey(f => f.ConditionalOnFieldId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ShopSetting>(entity =>
        {
            entity.HasIndex(s => s.Key).IsUnique();
        });
    }
}
