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
    public async Task<(bool success, string message)> CreateRegistration(string id, string host, int port)
    {
        var entity = new RouteEntity
        {
            Id = id,
            Host = host,
            Port = port
        };

        try
        {
            var existing = await repository.GetByIdAsync(id);
            if (existing != null)
            {
                existing.Host = host;
                existing.Port = port;
            }
            else
            {
                await repository.AddAsync(entity);
            }

            await repository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return (false, $"Error saving to DB: {ex.Message}");
        }

        var caddyRoute = MapToCaddyRoute(id, host, port);
        var caddySuccess = await caddyService.CreateRoute(caddyRoute);

        if (!caddySuccess)
        {
            return (false, "Saved to DB but failed to update Caddy.");
        }

        return (true, "Registration successful.");
    }

    /// <summary>
    /// Elimina un registro de ruta de la base de datos y de Caddy.
    /// </summary>
    /// <param name="id">ID del registro a eliminar.</param>
    public async Task<(bool success, string message)> DeleteRegistration(string id)
    {
        var caddySuccess = await caddyService.DeleteRoute(id);

        await repository.DeleteAsync(id);
        await repository.SaveChangesAsync();

        if (!caddySuccess)
            return (false, "Deleted from DB but Caddy reported an error or route didn't exist in Caddy.");

        return (true, "Deletion successful.");
    }

    public async Task<(bool success, int count)> RestoreAll()
    {
        var entities = await repository.GetAllAsync();
        var routeList = entities.ToList();

        if (!routeList.Any())
            return (true, 0);

        var routes = routeList.Select(e => MapToCaddyRoute(e.Id, e.Host, e.Port))
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

    private CaddyRoute MapToCaddyRoute(string id, string host, int port)
    {
        return new CaddyRoute
        {
            Id = id,
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
                    Upstreams = new List<CaddyUpstream> { new() { Dial = $"127.0.0.1:{port}" } },
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