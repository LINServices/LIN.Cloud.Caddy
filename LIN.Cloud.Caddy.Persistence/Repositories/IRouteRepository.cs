using LIN.Cloud.Caddy.Persistence.Models;

namespace LIN.Cloud.Caddy.Persistence.Repositories;

public interface IRouteRepository
{
    Task<IEnumerable<RouteEntity>> GetAllAsync();
    Task<RouteEntity?> GetByIdAsync(string id);
    Task<RouteEntity?> GetByHostAsync(string host);
    Task AddAsync(RouteEntity route);
    Task DeleteAsync(string id);
    Task SaveChangesAsync();
}