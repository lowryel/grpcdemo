using Microsoft.EntityFrameworkCore;

public class MyAppDbContext : DbContext
{
    public MyAppDbContext(DbContextOptions<MyAppDbContext> options)
        : base(options) { }

    public DbSet<GreetingLog> GreetingLogs { get; set; }
}
