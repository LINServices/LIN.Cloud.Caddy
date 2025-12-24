using LIN.Cloud.Caddy.Persistence;
using LIN.Cloud.Caddy.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "LIN Cloud Caddy Service", Version = "v1" });

    // Add Security Definition
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key authentication using the X-API-KEY header",
        Name = "X-API-KEY",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Authentication
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<LIN.Cloud.Caddy.Authentication.ApiKeyAuthenticationOptions, LIN.Cloud.Caddy.Authentication.ApiKeyAuthenticationHandler>("ApiKey", options =>
    {
        options.HeaderName = "X-API-KEY";
        options.ApiKey = builder.Configuration["Authentication:MasterKey"] ?? "master-secret-key-2025";
    });

// Register Persistence
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
builder.Services.AddPersistence(connectionString);

// Register Services
builder.Services.AddServices();

var app = builder.Build();

// Auto-migrate database on startup
var defaultKey = builder.Configuration["Authentication:DefaultApiKey"] ?? "default-pro-key-2025";
await app.Services.InitializePersistence(defaultKey);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();