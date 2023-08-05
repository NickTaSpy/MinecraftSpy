using Microsoft.EntityFrameworkCore;

namespace MinecraftSpy;

public class DatabaseContext : DbContext
{
    public DbSet<Subscription> Subscriptions { get; set; }

    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }
}
