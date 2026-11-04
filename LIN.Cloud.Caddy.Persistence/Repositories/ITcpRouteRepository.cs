using LIN.Cloud.Caddy.Persistence.Models;

namespace LIN.Cloud.Caddy.Persistence.Repositories;

public interface ITcpRouteRepository
{
    Task<IEnumerable<TcpRouteEntity>> GetAllAsync();
    Task<TcpRouteEntity?> GetByIdAsync(string id);
    Task<IEnumerable<TcpRouteEntity>> GetAllByListenPortAsync(int listenPort);
    Task AddAsync(TcpRouteEntity route);
    Task DeleteAsync(string id);
    Task UpdateStateAsync(string id, bool isActive);
    Task SaveChangesAsync();
}
