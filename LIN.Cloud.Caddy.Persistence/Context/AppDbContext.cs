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

        // Configuramos el índice en Host para búsquedas más rápidas
        modelBuilder.Entity<RouteEntity>()
            .HasIndex(r => r.Host);
    }
}