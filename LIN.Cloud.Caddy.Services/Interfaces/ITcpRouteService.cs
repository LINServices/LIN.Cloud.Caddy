using LIN.Cloud.Caddy.Persistence.Models;

namespace LIN.Cloud.Caddy.Services.Interfaces;

public interface ITcpRouteService
{
    /// <summary>
    /// Registra (o actualiza) un servicio TCP en la base de datos y lo sincroniza con Caddy (layer4).
    /// </summary>
    /// <param name="id">ID único del registro.</param>
    /// <param name="listenPort">Puerto público donde escuchará Caddy.</param>
    /// <param name="host">Host (SNI) que enruta al servicio; vacío = cualquier conexión en el puerto.</param>
    /// <param name="terminateTls">true: Caddy termina el TLS con su certificado (requiere host); false: passthrough.</param>
    /// <param name="port">Puerto del servicio destino.</param>
    /// <param name="target">IP o host del servicio destino.</param>
    /// <param name="protocol">Etiqueta informativa (postgres, mongo, redis...).</param>
    /// <param name="isActive">Indica si el servicio debe estar activo.</param>
    Task<(bool success, string message)> CreateRegistration(string id, int listenPort, string host, int port, string target, string protocol, bool terminateTls = false, bool isActive = true);

    /// <summary>
    /// Actualiza el estado activo/inactivo de un servicio TCP y sincroniza con Caddy.
    /// </summary>
    Task<(bool success, string message)> UpdateState(string id, bool isActive);

    /// <summary>
    /// Elimina un servicio TCP de la base de datos y de Caddy.
    /// </summary>
    Task<(bool success, string message)> DeleteRegistration(string id);

    /// <summary>
    /// Lista los servicios TCP registrados.
    /// </summary>
    Task<List<TcpRouteEntity>> GetAll();

    /// <summary>
    /// Restaura el app layer4 de Caddy a partir de los registros activos de la base de datos.
    /// </summary>
    Task<(bool success, int count)> RestoreAll();
}
