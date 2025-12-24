using System.Net;
using LIN.Cloud.Caddy.Services.Implementations;
using LIN.Cloud.Caddy.Services.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace LIN.Cloud.Caddy.Test;

public class CaddyServiceTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly HttpClient _httpClient;
    private readonly CaddyService _caddyService;

    public CaddyServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Caddy:AdminBaseUrl"]).Returns("http://localhost:2019");

        _httpClient = new HttpClient(_handlerMock.Object);
        _caddyService = new CaddyService(_httpClient, _configMock.Object);
    }

    [Fact]
    public async Task CreateRoute_Success_ReturnsTrue()
    {
        // Preparación
        var route = new CaddyRoute { Id = "test-route" };
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Acción
        var result = await _caddyService.CreateRoute(route);

        // Verificación
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteRoute_Success_ReturnsTrue()
    {
        // Preparación
        var id = "test-route";
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Acción
        var result = await _caddyService.DeleteRoute(id);

        // Verificación
        Assert.True(result);
    }

    [Fact]
    public async Task LoadConfig_Success_ReturnsTrue()
    {
        // Preparación
        var routes = new List<CaddyRoute> { new() { Id = "1" } };
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Acción
        var result = await _caddyService.LoadConfig(routes);

        // Verificación
        Assert.True(result);
    }
    [Fact]
    public async Task GetVersion_Success_ReturnsVersionString()
    {
        // Preparación
        var versionString = "v2.8.4";
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(versionString)
            });

        // Acción
        var result = await _caddyService.GetVersion();

        // Verificación
        Assert.Equal(versionString, result);
    }
}
