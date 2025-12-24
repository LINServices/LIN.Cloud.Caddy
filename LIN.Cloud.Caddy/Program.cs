using LIN.Cloud.Caddy.Extensions;
using LIN.Cloud.Caddy.Persistence;
using LIN.Cloud.Caddy.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Swagger configuration
builder.Services.AddSwaggerConfiguration();

// Configure Authentication
builder.Services.AddAuthConfiguration(builder.Configuration);

// Register Persistence
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
builder.Services.AddPersistence(connectionString);

// Register Services
builder.Services.AddServices();

var app = builder.Build();

// Auto-migrate database on startup
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