using LIN.Cloud.Caddy.Persistence.Context;
using LIN.Cloud.Caddy.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LIN.Cloud.Caddy.Persistence.Repositories.EF;

internal class TcpRouteRepository(AppDbContext context) : ITcpRouteRepository
{
    public async Task<IEnumerable<TcpRouteEntity>> GetAllAsync()
    {
        return await context.TcpRoutes.ToListAsync();
    }

    public async Task<TcpRouteEntity?> GetByIdAsync(string id)
    {
        return await context.TcpRoutes.FindAsync(id);
    }

    public async Task<IEnumerable<TcpRouteEntity>> GetAllByListenPortAsync(int listenPort)
    {
        return await context.TcpRoutes.Where(r => r.ListenPort == listenPort).ToListAsync();
    }

    public async Task AddAsync(TcpRouteEntity route)
    {
        await context.TcpRoutes.AddAsync(route);
    }

    public async Task DeleteAsync(string id)
    {
        var route = await context.TcpRoutes.FindAsync(id);
        if (route != null)
        {
            context.TcpRoutes.Remove(route);
        }
    }

    public async Task UpdateStateAsync(string id, bool isActive)
    {
        var route = await context.TcpRoutes.FindAsync(id);
        if (route != null)
        {
            route.IsActive = isActive;
        }
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
