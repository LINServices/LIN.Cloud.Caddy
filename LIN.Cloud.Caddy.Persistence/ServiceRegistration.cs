using LIN.Cloud.Caddy.Persistence.Context;
using LIN.Cloud.Caddy.Persistence.Repositories;
using LIN.Cloud.Caddy.Persistence.Repositories.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LIN.Cloud.Caddy.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        return services;
    }

    public static async Task InitializePersistence(this IServiceProvider serviceProvider, string defaultApiKey)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apiKeyRepo = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();

        db.Database.EnsureCreated();

        if (!await apiKeyRepo.AnyAsync())
        {
            await apiKeyRepo.AddAsync(new Models.ApiKeyEntity
            {
                Key = defaultApiKey,
                Description = "Default Initial Key",
                IsActive = true
            });
            await apiKeyRepo.SaveChangesAsync();
        }
    }
}