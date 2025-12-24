using LIN.Cloud.Caddy.Persistence.Models;
using LIN.Cloud.Caddy.Persistence.Repositories;
using LIN.Cloud.Caddy.Services.Interfaces;
using System.Security.Cryptography;

namespace LIN.Cloud.Caddy.Services.Implementations;

internal class ApiKeyService(IApiKeyRepository repository) : IApiKeyService
{
    /// <summary>
    /// Genera una nueva clave de API con una descripción.
    /// </summary>
    /// <param name="description">Descripción del propósito de la clave.</param>
    public async Task<ApiKeyEntity> GenerateKey(string description)
    {
        var key = GenerateSecureKey();
        var entity = new ApiKeyEntity
        {
            Key = key,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();

        return entity;
    }

    /// <summary>
    /// Obtiene todas las claves de API registradas.
    /// </summary>
    public async Task<List<ApiKeyEntity>> GetAllKeys()
    {
        return [.. await repository.GetAllAsync()];
    }

    /// <summary>
    /// Desactiva una clave de API existente.
    /// </summary>
    /// <param name="key">La clave de API a desactivar.</param>
    public async Task<bool> DeactivateKey(string key)
    {
        var entity = await repository.GetByKeyAsync(key);
        if (entity == null) return false;

        entity.IsActive = false;
        await repository.SaveChangesAsync();
        return true;
    }

    private static string GenerateSecureKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .TrimEnd('=');
    }
}