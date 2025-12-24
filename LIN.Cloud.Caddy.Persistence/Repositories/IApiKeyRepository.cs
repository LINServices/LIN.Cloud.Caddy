using LIN.Cloud.Caddy.Persistence.Models;

namespace LIN.Cloud.Caddy.Persistence.Repositories;

public interface IApiKeyRepository
{
    Task<IEnumerable<ApiKeyEntity>> GetAllAsync();
    Task<ApiKeyEntity?> GetByKeyAsync(string key);
    Task AddAsync(ApiKeyEntity apiKey);
    Task<bool> AnyAsync();
    Task SaveChangesAsync();
}