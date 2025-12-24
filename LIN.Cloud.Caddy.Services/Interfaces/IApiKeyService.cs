using LIN.Cloud.Caddy.Persistence.Models;

namespace LIN.Cloud.Caddy.Services.Interfaces;

public interface IApiKeyService
{
    /// <summary>
    /// Genera una nueva clave de API con una descripción.
    /// </summary>
    /// <param name="description">Descripción del propósito de la clave.</param>
    Task<ApiKeyEntity> GenerateKey(string description);

    /// <summary>
    /// Obtiene todas las claves de API registradas.
    /// </summary>
    Task<List<ApiKeyEntity>> GetAllKeys();

    /// <summary>
    /// Desactiva una clave de API existente.
    /// </summary>
    /// <param name="key">La clave de API a desactivar.</param>
    Task<bool> DeactivateKey(string key);
}
