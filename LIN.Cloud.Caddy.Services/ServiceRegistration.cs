using LIN.Cloud.Caddy.Services.Implementations;
using LIN.Cloud.Caddy.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LIN.Cloud.Caddy.Services;

public static class ServiceRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddHttpClient<ICaddyService, CaddyService>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();

        return services;
    }
}
