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
        services.AddScoped<ITcpRouteRepository, TcpRouteRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        return services;
    }

    public static async Task InitializePersistence(this IServiceProvider serviceProvider, string defaultApiKey)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apiKeyRepo = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();

        db.Database.EnsureCreated();

        // EnsureCreated no agrega tablas nuevas a una base de datos existente.
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[TcpRoutes]', N'U') IS NULL
BEGIN
    CREATE TABLE [TcpRoutes] (
        [Id]         nvarchar(450) NOT NULL,
        [ListenPort] int           NOT NULL,
        [Host]       nvarchar(450) NOT NULL CONSTRAINT [DF_TcpRoutes_Host] DEFAULT N'',
        [TerminateTls] bit         NOT NULL CONSTRAINT [DF_TcpRoutes_TerminateTls] DEFAULT 0,
        [Target]     nvarchar(max) NOT NULL,
        [Port]       int           NOT NULL,
        [Protocol]   nvarchar(max) NOT NULL,
        [CreatedAt]  datetime2     NOT NULL,
        [IsActive]   bit           NOT NULL,
        CONSTRAINT [PK_TcpRoutes] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_TcpRoutes_ListenPort_Host] ON [TcpRoutes] ([ListenPort], [Host]);
END
ELSE IF COL_LENGTH(N'TcpRoutes', N'Host') IS NULL
BEGIN
    ALTER TABLE [TcpRoutes] ADD [Host] nvarchar(450) NOT NULL CONSTRAINT [DF_TcpRoutes_Host] DEFAULT N'';
    DROP INDEX [IX_TcpRoutes_ListenPort] ON [TcpRoutes];
    CREATE UNIQUE INDEX [IX_TcpRoutes_ListenPort_Host] ON [TcpRoutes] ([ListenPort], [Host]);
END
IF COL_LENGTH(N'TcpRoutes', N'TerminateTls') IS NULL
    ALTER TABLE [TcpRoutes] ADD [TerminateTls] bit NOT NULL CONSTRAINT [DF_TcpRoutes_TerminateTls] DEFAULT 0;");

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