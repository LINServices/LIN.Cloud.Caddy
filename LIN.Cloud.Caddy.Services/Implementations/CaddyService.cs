using LIN.Cloud.Caddy.Services.Interfaces;
using LIN.Cloud.Caddy.Services.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace LIN.Cloud.Caddy.Services.Implementations;

internal class CaddyService(HttpClient httpClient, IConfiguration configuration) : ICaddyService
{

    private readonly string _caddyAdminBaseUrl = configuration["Caddy:AdminBaseUrl"] ?? "http://127.0.0.1:2019";
    private static readonly SemaphoreSlim _routesSemaphore = new(1, 1);

    /// <summary>
    /// Crea o actualiza una ruta en la configuración de Caddy (upsert atómico).
    /// Solo una ejecución concurrente está permitida a la vez.
    /// </summary>
    public async Task<bool> CreateRoute(CaddyRoute route)
    {
        await _routesSemaphore.WaitAsync();
        try
        {
            var urlRoutes = $"{_caddyAdminBaseUrl}/config/apps/http/servers/srv0/routes";
            var listResponse = await httpClient.GetAsync(urlRoutes);

            if (!listResponse.IsSuccessStatusCode)
            {
                await InitializeInfrastructure();
                var bootstrapResponse = await httpClient.PostAsJsonAsync(urlRoutes, route);
                return bootstrapResponse.IsSuccessStatusCode;
            }

            var routes = await listResponse.Content.ReadFromJsonAsync<List<CaddyRoute>>() ?? [];
            var hosts = route.Match.SelectMany(m => m.Host).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!UpsertRouteInList(routes, route, hosts))
                routes.Add(route);

            var patchResponse = await httpClient.PatchAsJsonAsync(urlRoutes, routes);
            return patchResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
        finally
        {
            _routesSemaphore.Release();
        }
    }

    /// <summary>
    /// Elimina una ruta de la configuración de Caddy por su host.
    /// Solo una ejecución concurrente está permitida a la vez.
    /// </summary>
    public async Task<bool> DeleteRoute(string host)
    {
        await _routesSemaphore.WaitAsync();
        try
        {
            var urlRoutes = $"{_caddyAdminBaseUrl}/config/apps/http/servers/srv0/routes";
            var listResponse = await httpClient.GetAsync(urlRoutes);

            if (!listResponse.IsSuccessStatusCode)
                return false;

            var routes = await listResponse.Content.ReadFromJsonAsync<List<CaddyRoute>>() ?? [];
            RemoveRouteFromList(routes, host);

            var patchResponse = await httpClient.PatchAsJsonAsync(urlRoutes, routes);
            return patchResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
        finally
        {
            _routesSemaphore.Release();
        }
    }

    /// <summary>
    /// Busca recursivamente (routes y handle subroutes) la ruta que coincide con los hosts
    /// y la reemplaza en su lugar. Devuelve true si se realizó el reemplazo.
    /// </summary>
    private static bool UpsertRouteInList(List<CaddyRoute> routes, CaddyRoute replacement, HashSet<string> hosts)
    {
        for (var i = 0; i < routes.Count; i++)
        {
            if (routes[i].Match.Any(m => m.Host.Any(h => hosts.Contains(h))))
            {
                routes[i] = replacement;
                return true;
            }

            foreach (var handle in routes[i].Handle)
            {
                if (handle.Routes is not null && UpsertRouteInList(handle.Routes, replacement, hosts))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Elimina recursivamente la primera ruta que coincide con el host dado.
    /// </summary>
    private static void RemoveRouteFromList(List<CaddyRoute> routes, string host)
    {
        for (var i = routes.Count - 1; i >= 0; i--)
        {
            if (routes[i].Match.Any(m => m.Host.Any(h => h.Equals(host, StringComparison.OrdinalIgnoreCase))))
            {
                routes.RemoveAt(i);
                return;
            }

            foreach (var handle in routes[i].Handle)
            {
                if (handle.Routes is not null)
                    RemoveRouteFromList(handle.Routes, host);
            }
        }
    }

    /// <summary>
    /// Carga una configuración completa en Caddy (reemplazo atómico).
    /// </summary>
    /// <param name="routes">Lista de rutas a incluir en la configuración.</param>
    public async Task<bool> LoadConfig(List<CaddyRoute> routes)
    {
        try
        {
            var url = $"{_caddyAdminBaseUrl}/load";

            var fullConfig = new
            {
                admin = new { listen = "0.0.0.0:2019" },
                apps = new
                {
                    http = new
                    {
                        servers = new
                        {
                            srv0 = new
                            {
                                listen = new[] { ":80", ":443" },
                                routes = routes
                            }
                        }
                    },
                    tls = new
                    {
                        certificates = new
                        {
                            automate = new[]
                             {
                               "linapps.online",
                               "*.linapps.online",
                               "*.linsites.qzz.io"
                             }
                        },
                        automation = new
                        {
                            policies = new[]
                         {
                           new
                           {
                             subjects = new[]
                             {
                                "linapps.online",
                                "*.linapps.online",
                                 "*.linsites.qzz.io"
                             },
                             issuers = new[]
                             {
                               new
                               {
                                 module = "acme",
                                 challenges = new
                                 {
                                   dns = new
                                   {
                                     provider = new
                                     {
                                       name = "cloudflare",
                                       api_token = "yoq22AxVEAQVNT3y4Cdw145i_WZHLyvZvlXJ9CxD"
                                     }
                                   }
                                 }
                               }
                             }
                           }
                         }
                        }
                    }
                }
            };

            var response = await httpClient.PostAsJsonAsync(url, fullConfig);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtiene la versión actual de Caddy.
    /// </summary>
    public async Task<string> GetVersion()
    {
        try
        {
            var url = $"{_caddyAdminBaseUrl}/version";
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var version = await response.Content.ReadAsStringAsync();
                return version;
            }
            return "Unknown (Caddy Not Responding)";
        }
        catch
        {
            return "Unknown (Network Error)";
        }
    }

    private async Task InitializeInfrastructure()
    {
        try
        {
            await httpClient.PutAsJsonAsync($"{_caddyAdminBaseUrl}/config/apps", new { });
            await httpClient.PutAsJsonAsync($"{_caddyAdminBaseUrl}/config/apps/http", new { servers = new { } });

            var srv0Config = new
            {
                listen = new[] { ":80", ":443" },
                routes = new object[] { }
            };
            await httpClient.PutAsJsonAsync($"{_caddyAdminBaseUrl}/config/apps/http/servers/srv0", srv0Config);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error de inicialización: {ex.Message}");
        }
    }
}