using System.ComponentModel.DataAnnotations;

namespace LIN.Cloud.Caddy.Persistence.Models;

/// <summary>
/// Servicio TCP crudo (Postgres, MongoDB, Redis...) publicado por Caddy (layer4) en un puerto propio.
/// </summary>
public class TcpRouteEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Puerto público donde escucha Caddy.
    /// </summary>
    public int ListenPort { get; set; }

    /// <summary>
    /// Host (SNI) que enruta a este servicio. Vacío = cualquier conexión en el puerto.
    /// Permite compartir un mismo puerto entre varios servicios (requiere TLS en el cliente).
    /// </summary>
    [MaxLength(450)]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// true = Caddy termina el TLS (certificado propio) y reenvía en claro al destino.
    /// false = passthrough: el TLS lo resuelve el destino.
    /// </summary>
    public bool TerminateTls { get; set; }

    [Required]
    public string Target { get; set; } = "127.0.0.1";

    /// <summary>
    /// Puerto del servicio destino.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Etiqueta informativa (postgres, mongo, redis, ...).
    /// </summary>
    public string Protocol { get; set; } = "tcp";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
