using LIN.Cloud.Caddy.Persistence.Models;
using LIN.Cloud.Caddy.Persistence.Repositories;
using LIN.Cloud.Caddy.Services.Interfaces;
using LIN.Cloud.Caddy.Services.Models;

namespace LIN.Cloud.Caddy.Services.Implementations;

internal class TcpRouteService(ITcpRouteRepository repository, ICaddyService caddyService) : ITcpRouteService
{
    // Puertos usados por Caddy (HTTP/HTTPS/Admin API) que no pueden publicarse como TCP.
    private static readonly int[] ReservedPorts = [80, 443, 2019];

    public async Task<(bool success, string message)> CreateRegistration(string id, int listenPort, string host, int port, string target, string protocol, bool terminateTls = false, bool isActive = true)
    {
        if (listenPort is < 1 or > 65535 || port is < 1 or > 65535)
            return (false, "Los puertos deben estar entre 1 y 65535.");

        if (ReservedPorts.Contains(listenPort))
            return (false, $"El puerto {listenPort} está reservado por Caddy.");

        host = (host ?? string.Empty).Trim().ToLowerInvariant();

        if (terminateTls && host.Length == 0)
            return (false, "TerminateTls requiere un host para emitir el certificado.");

        if (terminateTls && host.Contains('*'))
            return (false, "El host no puede contener comodines.");
        int? previousPort = null;

        try
        {
            var sameHost = (await repository.GetAllByListenPortAsync(listenPort))
                .FirstOrDefault(e => e.Host == host && e.Id != id);
            if (sameHost != null)
            {
                var scope = host.Length == 0 ? "sin host" : $"el host '{host}'";
                return (false, $"El puerto {listenPort} ya está en uso {scope} por el registro '{sameHost.Id}'.");
            }

            var existing = await repository.GetByIdAsync(id);
            if (existing != null)
            {
                if (existing.ListenPort != listenPort)
                    previousPort = existing.ListenPort;

                existing.ListenPort = listenPort;
                existing.Host = host;
                existing.TerminateTls = terminateTls;
                existing.Port = port;
                existing.Target = target;
                existing.Protocol = protocol;
                existing.IsActive = isActive;
            }
            else
            {
                await repository.AddAsync(new TcpRouteEntity
                {
                    Id = id,
                    ListenPort = listenPort,
                    Host = host,
                    TerminateTls = terminateTls,
                    Port = port,
                    Target = target,
                    Protocol = protocol,
                    IsActive = isActive
                });
            }

            await repository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return (false, $"Error al guardar en la base de datos: {ex.Message}");
        }

        var caddySuccess = await SyncPort(listenPort);
        if (previousPort != null)
            caddySuccess &= await SyncPort(previousPort.Value);

        if (!caddySuccess)
            return (false, "Se guardó en la base de datos pero falló la actualización en Caddy.");

        return (true, "Registro TCP completado con éxito.");
    }

    public async Task<(bool success, string message)> UpdateState(string id, bool isActive)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null)
            return (false, "Registro no encontrado.");

        var listenPort = entity.ListenPort;

        await repository.UpdateStateAsync(id, isActive);
        await repository.SaveChangesAsync();

        if (!await SyncPort(listenPort))
            return (false, "Se actualizó el estado en la base de datos, pero falló la actualización en Caddy.");

        return (true, "Estado actualizado con éxito.");
    }

    public async Task<(bool success, string message)> DeleteRegistration(string id)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null)
            return (false, "Registro no encontrado.");

        var listenPort = entity.ListenPort;

        await repository.DeleteAsync(id);
        await repository.SaveChangesAsync();

        if (!await SyncPort(listenPort))
            return (false, "Se eliminó de la base de datos, pero Caddy reportó un error.");

        return (true, "Eliminación exitosa.");
    }

    public async Task<List<TcpRouteEntity>> GetAll()
    {
        return [.. await repository.GetAllAsync()];
    }

    public async Task<(bool success, int count)> RestoreAll()
    {
        var active = (await repository.GetAllAsync())
            .Where(e => e.IsActive)
            .ToList();

        var servers = active
            .GroupBy(e => e.ListenPort)
            .ToDictionary(g => ServerName(g.Key), g => MapToCaddyServer(g.Key, g));

        var certs = await caddyService.EnsureCertificateHosts(TlsHosts(active));
        var success = await caddyService.ReplaceTcpServers(servers);

        return (success && certs, servers.Count);
    }

    /// <summary>
    /// Sincroniza con Caddy el server de un puerto a partir de sus registros activos:
    /// una ruta por host (SNI) y, si hay un registro sin host, una ruta por defecto al final.
    /// </summary>
    private async Task<bool> SyncPort(int listenPort)
    {
        var active = (await repository.GetAllByListenPortAsync(listenPort))
            .Where(e => e.IsActive)
            .ToList();

        var name = ServerName(listenPort);

        // El certificado debe existir antes de que Caddy acepte conexiones TLS para el host.
        if (!await caddyService.EnsureCertificateHosts(TlsHosts(active)))
            return false;

        return active.Count > 0
            ? await caddyService.UpsertTcpServer(name, MapToCaddyServer(listenPort, active))
            : await caddyService.DeleteTcpServer(name);
    }

    private static IEnumerable<string> TlsHosts(IEnumerable<TcpRouteEntity> entities) =>
        entities.Where(e => e.TerminateTls).Select(e => e.Host);

    private static string ServerName(int listenPort) => $"tcp-{listenPort}";

    private static List<CaddyTcpHandle> BuildHandlers(TcpRouteEntity e)
    {
        var handlers = new List<CaddyTcpHandle>();

        // Terminación TLS: Caddy hace el handshake y el destino recibe tráfico en claro.
        if (e.TerminateTls)
        {
            var tls = new CaddyTcpHandle { Handler = "tls" };

            // sslnegotiation=direct (PostgreSQL 17, Npgsql) exige ALPN "postgresql"; sin él el handshake aborta.
            if (string.Equals(e.Protocol, "postgres", StringComparison.OrdinalIgnoreCase))
                tls.ConnectionPolicies = [new CaddyTcpConnectionPolicy { Alpn = ["postgresql"] }];

            handlers.Add(tls);
        }

        handlers.Add(new CaddyTcpHandle
        {
            Upstreams = [new CaddyTcpUpstream { Dial = [$"{e.Target}:{e.Port}"] }]
        });

        return handlers;
    }

    private static CaddyTcpServer MapToCaddyServer(int listenPort, IEnumerable<TcpRouteEntity> entities)
    {
        // Las rutas con host van primero; la ruta sin host (cualquier conexión) actúa como fallback.
        var routes = entities
            .OrderBy(e => e.Host.Length == 0)
            .Select(e => new CaddyTcpRoute
            {
                Match = e.Host.Length == 0
                    ? null
                    : [new CaddyTcpMatch { Tls = new CaddyTcpTlsMatch { Sni = [e.Host] } }],
                Handle = BuildHandlers(e)
            })
            .ToList();

        return new CaddyTcpServer
        {
            Listen = [$":{listenPort}"],
            Routes = routes
        };
    }
}
