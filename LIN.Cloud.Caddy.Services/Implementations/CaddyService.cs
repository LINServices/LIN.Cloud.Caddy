using LIN.Cloud.Caddy.Services.Interfaces;
using LIN.Cloud.Caddy.Services.Models;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

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

            // /load reemplaza toda la configuración: se conserva el app layer4 (TCP) vigente.
            var layer4 = await GetLayer4Config();
            var tlsHosts = BaseTlsHosts.Concat((await GetTlsHosts() ?? []).Except(BaseTlsHosts)).ToList();

            var fullConfig = new
            {
                admin = new { listen = "0.0.0.0:2019" },
                apps = new
                {
                    layer4,
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
                    tls = BuildTlsApp(tlsHosts)
                }
            };

            var response = await httpClient.PostAsJsonAsync(url, fullConfig);
            var ss = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Dominios que siempre gestiona Caddy (ver también LoadConfig).
    private static readonly string[] BaseTlsHosts =
    [
        "linapps.xyz",
        "*.linapps.xyz",
        "*.linsites.qzz.io",
        "*.db.linsites.qzz.io",
        // Motores de datos publicados por SNI: <id>.<motor>.linsites.qzz.io (un comodín por motor).
        "*.postgres.linsites.qzz.io",
        "*.mysql.linsites.qzz.io",
        "*.mongo.linsites.qzz.io",
        "*.redis.linsites.qzz.io",
        "*.valkey.linsites.qzz.io",
        "*.rabbitmq.linsites.qzz.io"
    ];

    private static object BuildTlsApp(List<string> hosts)
    {
        return new
        {
                        certificates = new
                        {
                            automate = hosts
                        },
                        automation = new
                        {
                            policies = new[]
                         {
                           new
                           {
                             subjects = hosts,
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
                    };
    }

    /// <summary>
    /// Hosts con certificado automático en Caddy, o null si el app tls no existe.
    /// </summary>
    private async Task<List<string>?> GetTlsHosts()
    {
        try
        {
            var response = await httpClient.GetAsync($"{_caddyAdminBaseUrl}/config/apps/tls/certificates/automate");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<List<string>>();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsCovered(string host, IEnumerable<string> managed)
    {
        var dot = host.IndexOf('.');
        var wildcard = dot > 0 ? "*" + host[dot..] : null;

        return managed.Any(m => m.Equals(host, StringComparison.OrdinalIgnoreCase)
            || (wildcard != null && m.Equals(wildcard, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Asegura que Caddy gestione un certificado (ACME DNS) para cada host indicado.
    /// </summary>
    public async Task<bool> EnsureCertificateHosts(IEnumerable<string> hosts)
    {
        var needed = hosts
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (needed.Count == 0)
            return true;

        await _routesSemaphore.WaitAsync();
        try
        {
            var current = await GetTlsHosts();

            if (current is null)
            {
                // Sin app tls (Caddy sin LoadConfig previo): se crea con la base y los hosts pedidos.
                var all = BaseTlsHosts.Concat(needed).Distinct().ToList();
                return await SetConfig($"{_caddyAdminBaseUrl}/config/apps/tls", BuildTlsApp(all));
            }

            foreach (var host in needed.Where(h => !IsCovered(h, current)))
            {
                var cert = await httpClient.PostAsJsonAsync($"{_caddyAdminBaseUrl}/config/apps/tls/certificates/automate", host);
                if (!cert.IsSuccessStatusCode)
                    return false;

                var subject = await httpClient.PostAsJsonAsync($"{_caddyAdminBaseUrl}/config/apps/tls/automation/policies/0/subjects", host);
                if (!subject.IsSuccessStatusCode)
                    return false;
            }

            return true;
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
    /// Establece un valor en la Admin API de Caddy. PATCH reemplaza una ruta existente;
    /// si no existe, PUT la crea (PUT sobre una ruta existente responde 409).
    /// </summary>
    private async Task<bool> SetConfig(string url, object value)
    {
        var patch = await httpClient.PatchAsJsonAsync(url, value);
        if (patch.IsSuccessStatusCode)
            return true;

        var put = await httpClient.PutAsJsonAsync(url, value);
        return put.IsSuccessStatusCode;
    }

    /// <summary>
    /// Crea o reemplaza un server TCP (layer4) en Caddy.
    /// </summary>
    public async Task<bool> UpsertTcpServer(string name, CaddyTcpServer server)
    {
        await _routesSemaphore.WaitAsync();
        try
        {
            if (await SetConfig($"{_caddyAdminBaseUrl}/config/apps/layer4/servers/{Uri.EscapeDataString(name)}", server))
                return true;

            // El app layer4 aún no existe: se crea junto con el server.
            return await SetConfig($"{_caddyAdminBaseUrl}/config/apps/layer4", new { servers = new Dictionary<string, CaddyTcpServer> { [name] = server } });
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
    /// Elimina un server TCP (layer4) de Caddy. Si no existe se considera exitoso.
    /// </summary>
    public async Task<bool> DeleteTcpServer(string name)
    {
        await _routesSemaphore.WaitAsync();
        try
        {
            var url = $"{_caddyAdminBaseUrl}/config/apps/layer4/servers/{Uri.EscapeDataString(name)}";
            var response = await httpClient.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
                return true;

            // Caddy responde error cuando la ruta no existe; se confirma consultándola.
            var check = await httpClient.GetAsync(url);
            return check.StatusCode == HttpStatusCode.NotFound
                || (check.IsSuccessStatusCode && (await check.Content.ReadAsStringAsync()).Trim() == "null");
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
    /// Reemplaza todos los servers TCP (layer4) de Caddy.
    /// </summary>
    public async Task<bool> ReplaceTcpServers(Dictionary<string, CaddyTcpServer> servers)
    {
        await _routesSemaphore.WaitAsync();
        try
        {
            return await SetConfig($"{_caddyAdminBaseUrl}/config/apps/layer4", new { servers });
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

    private async Task<JsonNode> GetLayer4Config()
    {
        try
        {
            var response = await httpClient.GetAsync($"{_caddyAdminBaseUrl}/config/apps/layer4");
            if (response.IsSuccessStatusCode)
            {
                var node = await response.Content.ReadFromJsonAsync<JsonNode>();
                if (node is JsonObject)
                    return node;
            }
        }
        catch
        {
            // Sin layer4 vigente (o Caddy sin el módulo): se parte de un app vacío.
        }

        return new JsonObject { ["servers"] = new JsonObject() };
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