using LIN.Cloud.Caddy.Persistence.Context;
using LIN.Cloud.Caddy.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LIN.Cloud.Caddy.Persistence.Repositories.EF;

internal class ApiKeyRepository(AppDbContext context) : IApiKeyRepository
{
    public async Task<IEnumerable<ApiKeyEntity>> GetAllAsync()
    {
        return await context.ApiKeys.ToListAsync();
    }

    public async Task<ApiKeyEntity?> GetByKeyAsync(string key)
    {
        return await context.ApiKeys.FindAsync(key);
    }

    public async Task AddAsync(ApiKeyEntity apiKey)
    {
        await context.ApiKeys.AddAsync(apiKey);
    }

    public async Task<bool> AnyAsync()
    {
        return await context.ApiKeys.AnyAsync();
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}