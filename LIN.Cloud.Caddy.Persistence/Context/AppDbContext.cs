using LIN.Cloud.Caddy.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LIN.Cloud.Caddy.Persistence.Context;

internal class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<RouteEntity> Routes => Set<RouteEntity>();
    public DbSet<ApiKeyEntity> ApiKeys => Set<ApiKeyEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure index on Host for faster lookups
        modelBuilder.Entity<RouteEntity>()
            .HasIndex(r => r.Host);
    }
}