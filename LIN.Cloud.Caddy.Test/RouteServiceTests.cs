using LIN.Cloud.Caddy.Persistence.Models;
using LIN.Cloud.Caddy.Persistence.Repositories;
using LIN.Cloud.Caddy.Services.Implementations;
using LIN.Cloud.Caddy.Services.Interfaces;
using LIN.Cloud.Caddy.Services.Models;
using Moq;
using Xunit;

namespace LIN.Cloud.Caddy.Test;

public class RouteServiceTests
{
    private readonly Mock<IRouteRepository> _repositoryMock;
    private readonly Mock<ICaddyService> _caddyServiceMock;
    private readonly RouteService _routeService;

    public RouteServiceTests()
    {
        _repositoryMock = new Mock<IRouteRepository>();
        _caddyServiceMock = new Mock<ICaddyService>();
        _routeService = new RouteService(_repositoryMock.Object, _caddyServiceMock.Object);
    }

    [Fact]
    public async Task CreateRegistration_Successful_ReturnsSuccess()
    {
        // Preparación
        var id = "test-id";
        var host = "test.domain.com";
        var port = 8080;

        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((RouteEntity?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<RouteEntity>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _caddyServiceMock.Setup(c => c.CreateRoute(It.IsAny<CaddyRoute>())).ReturnsAsync(true);

        // Acción
        var result = await _routeService.CreateRegistration(id, host, port);

        // Verificación
        Assert.True(result.success);
        Assert.Equal("Registro completado con éxito.", result.message);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<RouteEntity>(e => e.Id == id && e.Host == host && e.Port == port)), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _caddyServiceMock.Verify(c => c.CreateRoute(It.Is<CaddyRoute>(r => r.Id == id)), Times.Once);
    }

    [Fact]
    public async Task CreateRegistration_ExistingRoute_UpdatesAndReturnsSuccess()
    {
        // Preparación
        var id = "test-id";
        var host = "new.domain.com";
        var port = 9090;
        var existingEntity = new RouteEntity { Id = id, Host = "old.domain.com", Port = 8080 };

        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _caddyServiceMock.Setup(c => c.CreateRoute(It.IsAny<CaddyRoute>())).ReturnsAsync(true);

        // Acción
        var result = await _routeService.CreateRegistration(id, host, port);

        // Verificación
        Assert.True(result.success);
        Assert.Equal(host, existingEntity.Host);
        Assert.Equal(port, existingEntity.Port);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<RouteEntity>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateRegistration_CaddyFailure_ReturnsFailure()
    {
        // Preparación
        var id = "test-id";
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((RouteEntity?)null);
        _caddyServiceMock.Setup(c => c.CreateRoute(It.IsAny<CaddyRoute>())).ReturnsAsync(false);

        // Acción
        var result = await _routeService.CreateRegistration(id, "host", 80);

        // Verificación
        Assert.False(result.success);
        Assert.Equal("Se guardó en la base de datos pero falló la actualización en Caddy.", result.message);
    }

    [Fact]
    public async Task DeleteRegistration_Successful_ReturnsSuccess()
    {
        // Preparación
        var id = "test-id";
        _caddyServiceMock.Setup(c => c.DeleteRoute(id)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Acción
        var result = await _routeService.DeleteRegistration(id);

        // Verificación
        Assert.True(result.success);
        _caddyServiceMock.Verify(c => c.DeleteRoute(id), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task RestoreAll_CallsLoadConfig()
    {
        // Preparación
        var entities = new List<RouteEntity>
        {
            new() { Id = "1", Host = "h1", Port = 1 },
            new() { Id = "2", Host = "h2", Port = 2 }
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);
        _caddyServiceMock.Setup(c => c.LoadConfig(It.IsAny<List<CaddyRoute>>())).ReturnsAsync(true);

        // Acción
        var result = await _routeService.RestoreAll();

        // Verificación
        Assert.True(result.success);
        Assert.Equal(2, result.count);
        _caddyServiceMock.Verify(c => c.LoadConfig(It.Is<List<CaddyRoute>>(l => l.Count == 2)), Times.Once);
    }
    [Fact]
    public async Task GetCaddyVersion_CallsCaddyService()
    {
        // Preparación
        var expectedVersion = "v2.8.4";
        _caddyServiceMock.Setup(c => c.GetVersion()).ReturnsAsync(expectedVersion);

        // Acción
        var result = await _routeService.GetCaddyVersion();

        // Verificación
        Assert.Equal(expectedVersion, result);
        _caddyServiceMock.Verify(c => c.GetVersion(), Times.Once);
    }
}
