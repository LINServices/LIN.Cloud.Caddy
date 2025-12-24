using LIN.Cloud.Caddy.Persistence.Context;
using LIN.Cloud.Caddy.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LIN.Cloud.Caddy.Persistence.Repositories.EF;

internal class RouteRepository(AppDbContext context) : IRouteRepository
{
    public async Task<IEnumerable<RouteEntity>> GetAllAsync()
    {
        return await context.Routes.ToListAsync();
    }

    public async Task<RouteEntity?> GetByIdAsync(string id)
    {
        return await context.Routes.FindAsync(id);
    }

    public async Task<RouteEntity?> GetByHostAsync(string host)
    {
        return await context.Routes.FirstOrDefaultAsync(r => r.Host == host);
    }

    public async Task AddAsync(RouteEntity route)
    {
        await context.Routes.AddAsync(route);
    }

    public async Task DeleteAsync(string id)
    {
        var route = await context.Routes.FindAsync(id);
        if (route != null)
        {
            context.Routes.Remove(route);
        }
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}