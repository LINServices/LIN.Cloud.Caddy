using LIN.Cloud.Caddy.Authentication;

namespace LIN.Cloud.Caddy.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddAuthConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication("ApiKey")
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", options =>
            {
                options.HeaderName = "X-API-KEY";
                options.ApiKey = configuration["Authentication:MasterKey"] ?? "master-secret-key-2025";
            });

        return services;
    }
}
