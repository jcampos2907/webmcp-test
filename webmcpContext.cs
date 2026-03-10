using Microsoft.EntityFrameworkCore;

public class webmcpContext(DbContextOptions<webmcpContext> options) : DbContext(options)
{
    public DbSet<webmcp.Models.Bike> Bike { get; set; } = default!;
}
