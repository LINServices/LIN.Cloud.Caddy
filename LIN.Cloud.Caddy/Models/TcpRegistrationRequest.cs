namespace LIN.Cloud.Caddy.Models;

/// <param name="Id">ID único del registro.</param>
/// <param name="ListenPort">Puerto público donde escuchará Caddy.</param>
/// <param name="Port">Puerto del servicio destino.</param>
/// <param name="Target">IP o host del servicio destino.</param>
/// <param name="Host">Host (SNI) que enruta al servicio. Vacío = cualquier conexión en el puerto. Varios hosts pueden compartir puerto (el cliente debe usar TLS).</param>
/// <param name="TerminateTls">true: Caddy termina el TLS con su propio certificado y reenvía en claro al destino (requiere Host). false: passthrough.</param>
/// <param name="Protocol">Etiqueta informativa (postgres, mongo, redis...).</param>
public record TcpRegistrationRequest(string Id, int ListenPort, int Port, string Target = "127.0.0.1", string Host = "", string Protocol = "tcp", bool TerminateTls = false, bool IsActive = true);
