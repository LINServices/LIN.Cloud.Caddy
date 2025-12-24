using LIN.Cloud.Caddy.Services.Interfaces;
using LIN.Cloud.Caddy.Services.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace LIN.Cloud.Caddy.Services.Implementations;

internal class CaddyService(HttpClient httpClient, IConfiguration configuration) : ICaddyService
{

    private readonly string _caddyAdminBaseUrl = configuration["Caddy:AdminBaseUrl"] ?? "http://127.0.0.1:2019";

    /// <summary>
    /// Crea una nueva ruta en la configuración de Caddy.
    /// </summary>
    /// <param name="route">El modelo de la ruta de Caddy a crear.</param>
    public async Task<bool> CreateRoute(CaddyRoute route)
    {
        try
        {
            var urlId = $"{_caddyAdminBaseUrl}/id/{route.Id}";
            var updateResponse = await httpClient.PutAsJsonAsync(urlId, route);
            if (updateResponse.IsSuccessStatusCode)
                return true;

            var urlRoutes = $"{_caddyAdminBaseUrl}/config/apps/http/servers/srv0/routes";
            var response = await httpClient.PostAsJsonAsync(urlRoutes, route);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                if (error.Contains("invalid traversal path at: config/apps/http"))
                {
                    await InitializeInfrastructure();
                    response = await httpClient.PostAsJsonAsync(urlRoutes, route);
                }
                else if (error.Contains("key already exists: routes"))
                {
                    response = await httpClient.PutAsJsonAsync(urlId, route);
                }
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Elimina una ruta de la configuración de Caddy por su ID.
    /// </summary>
    /// <param name="id">El ID de la ruta a eliminar.</param>
    public async Task<bool> DeleteRoute(string id)
    {
        try
        {
            var url = $"{_caddyAdminBaseUrl}/id/{id}";
            var response = await httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
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
            System.Console.WriteLine($"Initialization Error: {ex.Message}");
        }
    }
}