using LIN.Cloud.Caddy.Extensions;
using LIN.Cloud.Caddy.Persistence;
using LIN.Cloud.Caddy.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configuración de Swagger
builder.Services.AddSwaggerConfiguration();

// Configuración de Autenticación
builder.Services.AddAuthConfiguration(builder.Configuration);

// Registro de Persistencia
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
builder.Services.AddPersistence(connectionString);

// Registro de Servicios
builder.Services.AddServices();

var app = builder.Build();

// Migración automática de la base de datos al iniciar
var defaultKey = builder.Configuration["Authentication:DefaultApiKey"] ?? "default-pro-key-2025";
await app.Services.InitializePersistence(defaultKey);

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();