using Microsoft.EntityFrameworkCore;
using BikePOS.Models;

namespace BikePOS.Data;

public class BikePosContext(DbContextOptions<BikePosContext> options) : DbContext(options)
{
    /// <summary>Set after creation to enable tenant query filters. Null = no filtering (superadmin/system).</summary>
    public int? CurrentStoreId { get; set; }

    // Tenant hierarchy
    public DbSet<Conglomerate> Conglomerate { get; set; } = default!;
    public DbSet<Company> Company { get; set; } = default!;
    public DbSet<Store> Store { get; set; } = default!;
    public DbSet<AppUser> AppUser { get; set; } = default!;
    public DbSet<StoreUser> StoreUser { get; set; } = default!;

    // Domain entities
    public DbSet<Component> Component { get; set; } = default!;
    public DbSet<ServiceTicket> ServiceTicket { get; set; } = default!;
    public DbSet<Mechanic> Mechanic { get; set; } = default!;
    public DbSet<Service> Service { get; set; } = default!;
    public DbSet<Product> Product { get; set; } = default!;
    public DbSet<TicketProduct> TicketProduct { get; set; } = default!;
    public DbSet<Charge> Charge { get; set; } = default!;
    public DbSet<Customer> Customer { get; set; } = default!;
    public DbSet<MetaFieldDefinition> MetaFieldDefinition { get; set; } = default!;
    public DbSet<CustomerMetaValue> CustomerMetaValue { get; set; } = default!;
    public DbSet<EntityMetaValue> EntityMetaValue { get; set; } = default!;
    public DbSet<ShopSetting> ShopSetting { get; set; } = default!;

    public override int SaveChanges()
    {
        SetUpdatedAt();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetUpdatedAt();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetUpdatedAt()
    {
        foreach (var entry in ChangeTracker.Entries<ServiceTicket>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasOne(c => c.Customer)
                .WithMany(cu => cu.Components)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ServiceTicket>(entity =>
        {
            entity.HasOne(t => t.Component)
                .WithMany()
                .HasForeignKey(t => t.ComponentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

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

            entity.HasOne(f => f.Company).WithMany().HasForeignKey(f => f.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(f => new { f.EntityType, f.Key, f.CompanyId }).IsUnique();
        });

        modelBuilder.Entity<EntityMetaValue>(entity =>
        {
            entity.HasOne(mv => mv.MetaFieldDefinition)
                .WithMany()
                .HasForeignKey(mv => mv.MetaFieldDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(mv => new { mv.EntityType, mv.EntityId, mv.MetaFieldDefinitionId }).IsUnique();
        });

        modelBuilder.Entity<ShopSetting>(entity =>
        {
            entity.HasIndex(s => new { s.StoreId, s.Key }).IsUnique();
        });

        // Tenant hierarchy
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasOne(c => c.Conglomerate)
                .WithMany(g => g.Companies)
                .HasForeignKey(c => c.ConglomerateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasOne(s => s.Company)
                .WithMany(c => c.Stores)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoreUser>(entity =>
        {
            entity.HasOne(su => su.AppUser)
                .WithMany(u => u.StoreUsers)
                .HasForeignKey(su => su.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(su => su.Store)
                .WithMany(s => s.StoreUsers)
                .HasForeignKey(su => su.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(su => new { su.AppUserId, su.StoreId }).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(u => u.ExternalSubjectId).IsUnique();
        });

        // Global tenant query filters — when CurrentStoreId is set, only return data for that store
        modelBuilder.Entity<Customer>().HasQueryFilter(e => CurrentStoreId == null || e.StoreId == CurrentStoreId);
        modelBuilder.Entity<Component>().HasQueryFilter(e => CurrentStoreId == null || e.StoreId == CurrentStoreId);
        modelBuilder.Entity<ServiceTicket>().HasQueryFilter(e => CurrentStoreId == null || e.StoreId == CurrentStoreId);
        modelBuilder.Entity<Mechanic>().HasQueryFilter(e => CurrentStoreId == null || e.StoreId == CurrentStoreId);
        modelBuilder.Entity<Service>().HasQueryFilter(e => CurrentStoreId == null || e.StoreId == CurrentStoreId);
        modelBuilder.Entity<Product>().HasQueryFilter(e => CurrentStoreId == null || e.StoreId == CurrentStoreId);
        modelBuilder.Entity<Charge>().HasQueryFilter(e => CurrentStoreId == null || e.StoreId == CurrentStoreId);
        modelBuilder.Entity<ShopSetting>().HasQueryFilter(e => CurrentStoreId == null || e.StoreId == CurrentStoreId);
        // MetaFieldDefinition: no global query filter — scoped explicitly by CompanyId (company-wide) or ConglomerateId (org-level)
    }
}
