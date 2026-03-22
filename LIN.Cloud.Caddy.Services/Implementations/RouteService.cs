using LIN.Cloud.Caddy.Persistence.Models;
using LIN.Cloud.Caddy.Persistence.Repositories;
using LIN.Cloud.Caddy.Services.Interfaces;
using LIN.Cloud.Caddy.Services.Models;

namespace LIN.Cloud.Caddy.Services.Implementations;

internal class RouteService(IRouteRepository repository, ICaddyService caddyService) : IRouteService
{
    /// <summary>
    /// Crea un nuevo registro de ruta en la base de datos y lo sincroniza con Caddy.
    /// </summary>
    /// <param name="id">ID único del registro.</param>
    /// <param name="host">Host o dominio de la ruta.</param>
    /// <param name="port">Puerto del servicio destino.</param>
    /// <param name="target">IP o host del servicio destino.</param>
    public async Task<(bool success, string message)> CreateRegistration(string id, string host, int port, string target)
    {
        var entity = new RouteEntity
        {
            Id = id,
            Host = host,
            Port = port,
            Target = target
        };

        try
        {
            var existing = await repository.GetByIdAsync(id);
            if (existing != null)
            {
                existing.Host = host;
                existing.Port = port;
                existing.Target = target;
            }
            else
            {
                await repository.AddAsync(entity);
            }

            await repository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return (false, $"Error al guardar en la base de datos: {ex.Message}");
        }

        var hostEntities = await repository.GetAllByHostAsync(host);
        var caddyRoute = MapToCaddyRoute(host, hostEntities);
        var caddySuccess = await caddyService.CreateRoute(caddyRoute);

        if (!caddySuccess)
            return (false, "Se guardó en la base de datos pero falló la actualización en Caddy.");

        return (true, "Registro completado con éxito.");
    }

    /// <summary>
    /// Elimina un registro de ruta de la base de datos y de Caddy.
    /// </summary>
    /// <param name="id">ID del registro a eliminar.</param>
    public async Task<(bool success, string message)> DeleteRegistration(string id)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null)
            return (false, "Registro no encontrado.");

        var host = entity.Host;

        await repository.DeleteAsync(id);
        await repository.SaveChangesAsync();

        var remaining = (await repository.GetAllByHostAsync(host)).ToList();

        bool caddySuccess;
        if (remaining.Count > 0)
        {
            var caddyRoute = MapToCaddyRoute(host, remaining);
            caddySuccess = await caddyService.CreateRoute(caddyRoute);
        }
        else
        {
            caddySuccess = await caddyService.DeleteRoute(host);
        }

        if (!caddySuccess)
            return (false, "Se eliminó de la base de datos, pero Caddy reportó un error o la ruta no existía en Caddy.");

        return (true, "Eliminación exitosa.");
    }

    public async Task<(bool success, int count)> RestoreAll()
    {
        var entities = await repository.GetAllAsync();
        var routeList = entities.ToList();

        if (routeList.Count == 0)
            return (true, 0);

        var routes = routeList
            .GroupBy(e => e.Host)
            .Select(g => MapToCaddyRoute(g.Key, g))
            .ToList();

        var success = await caddyService.LoadConfig(routes);

        return (success, routes.Count);
    }

    /// <summary>
    /// Obtiene la versión de Caddy.
    /// </summary>
    public async Task<string> GetCaddyVersion()
    {
        return await caddyService.GetVersion();
    }

    private static CaddyRoute MapToCaddyRoute(string host, IEnumerable<RouteEntity> entities)
    {
        return new CaddyRoute
        {
            Id = host,
            Match = new List<CaddyMatch> { new() { Host = new List<string> { host } } },
            Handle = new List<CaddyHandle>
            {
                new()
                {
                    Handler = "headers",
                    Response = new CaddyResponse
                    {
                        Set = new Dictionary<string, List<string>>
                        {
                            { "X-Cloud-provider", new List<string> { "LIN Cloud Platform" } }
                        },
                        Delete = new List<string> { "Via" },
                        Deferred = true
                    }
                },
                new()
                {
                    Handler = "reverse_proxy",
                    Upstreams = entities.Select(e => new CaddyUpstream { Dial = $"{e.Target}:{e.Port}" }).ToList(),
                    Headers = new CaddyHeaders
                    {
                        Request = new CaddyHeaderAction { Delete = new List<string> { "Via" } },
                        Response = new CaddyHeaderAction { Delete = new List<string> { "Via" } }
                    }
                }
            }
        };
    }
}