using LIN.Cloud.Caddy.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace LIN.Cloud.Caddy.Authentication;

/// <summary>
/// Opciones para la autenticación basada en API Key.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Esquema por defecto.
    /// </summary>
    public const string DefaultScheme = "ApiKey";

    /// <summary>
    /// Nombre del esquema.
    /// </summary>
    public string Scheme => DefaultScheme;

    /// <summary>
    /// Nombre del encabezado HTTP que contiene la API Key.
    /// </summary>
    public string HeaderName { get; set; } = "X-API-KEY";

    /// <summary>
    /// Master Key permitida (configurada en la aplicación).
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// Manejador de autenticación personalizado para validar solicitudes mediante API Key.
/// </summary>
/// <remarks>
/// Valida la clave contra una 'Master Key' estática o contra las claves registradas en la base de datos.
/// </remarks>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyRepository apiKeyRepo) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    /// <summary>
    /// Procesa la autenticación de la solicitud actual.
    /// </summary>
    /// <returns>Resultado de la autenticación.</returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Intentar obtener el encabezado de la API Key.
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        // 2. Validar que la clave no esté vacía.
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.NoResult();
        }

        // 3. Verificar si es la 'Master Key' (Administrador).
        if (!string.IsNullOrEmpty(Options.ApiKey) && Options.ApiKey == providedApiKey)
        {
            var claims = new[] {
                new Claim(ClaimTypes.Name, "Master Administrator"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("auth_type", "master_key")
            };

            var identity = new ClaimsIdentity(claims, Options.Scheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Options.Scheme);

            return AuthenticateResult.Success(ticket);
        }

        // 4. Validar contra la base de datos para claves regulares.
        try
        {
            var keyEntity = await apiKeyRepo.GetByKeyAsync(providedApiKey);

            if (keyEntity != null && keyEntity.IsActive)
            {
                Logger.LogInformation("Autenticación exitosa para la clave: {Description}.", keyEntity.Description);

                var claims = new[] {
                    new Claim(ClaimTypes.Name, keyEntity.Description),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("key_description", keyEntity.Description),
                    new Claim("auth_type", "database_key")
                };

                var identity = new ClaimsIdentity(claims, Options.Scheme);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Options.Scheme);

                return AuthenticateResult.Success(ticket);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error crítico al validar la API Key en la base de datos.");
            return AuthenticateResult.Fail("Error interno al validar la clave de seguridad.");
        }

        Logger.LogWarning("Intento de acceso con API Key inválida o inactiva.");
        return AuthenticateResult.Fail("Invalid or inactive API Key.");
    }
}
