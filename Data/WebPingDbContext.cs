using Microsoft.EntityFrameworkCore;
using WebPing.Models;

namespace WebPing.Data;

public class WebPingDbContext : DbContext
{
    public WebPingDbContext(DbContextOptions<WebPingDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<PushEndpoint> PushEndpoints { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Username);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Topics)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.Username)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.PushEndpoints)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.Username)
            .OnDelete(DeleteBehavior.Cascade);

        // Topic configuration
        modelBuilder.Entity<Topic>()
            .HasKey(t => t.Name);

        // PushEndpoint configuration
        modelBuilder.Entity<PushEndpoint>()
            .HasKey(p => p.Id);
    }
}
