namespace LIN.Cloud.Caddy.Services.Interfaces;

public interface IRouteService
{
    /// <summary>
    /// Crea un nuevo registro de ruta en la base de datos y lo sincroniza con Caddy.
    /// </summary>
    /// <param name="id">ID único del registro.</param>
    /// <param name="host">Host o dominio de la ruta.</param>
    /// <param name="port">Puerto del servicio destino.</param>
    Task<(bool success, string message)> CreateRegistration(string id, string host, int port);

    /// <summary>
    /// Elimina un registro de ruta de la base de datos y de Caddy.
    /// </summary>
    /// <param name="id">ID del registro a eliminar.</param>
    Task<(bool success, string message)> DeleteRegistration(string id);

    /// <summary>
    /// Restaura toda la configuración de Caddy basándose en los registros de la base de datos.
    /// </summary>
    Task<(bool success, int count)> RestoreAll();

    /// <summary>
    /// Obtiene la versión de Caddy.
    /// </summary>
    Task<string> GetCaddyVersion();
}
