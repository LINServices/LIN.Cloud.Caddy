using LIN.Cloud.Caddy.Services.Models;

namespace LIN.Cloud.Caddy.Services.Interfaces;

public interface ICaddyService
{
    /// <summary>
    /// Crea una nueva ruta en la configuración de Caddy.
    /// </summary>
    /// <param name="route">El modelo de la ruta de Caddy a crear.</param>
    Task<bool> CreateRoute(CaddyRoute route);

    /// <summary>
    /// Elimina una ruta de la configuración de Caddy por su ID.
    /// </summary>
    /// <param name="id">El ID de la ruta a eliminar.</param>
    Task<bool> DeleteRoute(string id);

    /// <summary>
    /// Carga una configuración completa en Caddy (reemplazo atómico).
    /// </summary>
    /// <param name="routes">Lista de rutas a incluir en la configuración.</param>
    Task<bool> LoadConfig(List<CaddyRoute> routes);

    /// <summary>
    /// Crea o reemplaza un server TCP (layer4) en Caddy.
    /// </summary>
    Task<bool> UpsertTcpServer(string name, CaddyTcpServer server);

    /// <summary>
    /// Elimina un server TCP (layer4) de Caddy. Si no existe se considera exitoso.
    /// </summary>
    Task<bool> DeleteTcpServer(string name);

    /// <summary>
    /// Reemplaza todos los servers TCP (layer4) de Caddy.
    /// </summary>
    Task<bool> ReplaceTcpServers(Dictionary<string, CaddyTcpServer> servers);

    /// <summary>
    /// Asegura que Caddy gestione un certificado automático para cada host indicado.
    /// </summary>
    Task<bool> EnsureCertificateHosts(IEnumerable<string> hosts);

    /// <summary>
    /// Obtiene la versión actual de Caddy.
    /// </summary>
    Task<string> GetVersion();
}
