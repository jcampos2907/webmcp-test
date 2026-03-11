using Microsoft.EntityFrameworkCore;

namespace BikePOS.Data;

public class BikePosContext(DbContextOptions<BikePosContext> options) : DbContext(options)
{
    public DbSet<BikePOS.Models.Bike> Bike { get; set; } = default!;
    public DbSet<BikePOS.Models.ServiceTicket> ServiceTicket { get; set; } = default!;
}
