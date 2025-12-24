using LIN.Cloud.Caddy.Persistence.Models;
using LIN.Cloud.Caddy.Persistence.Repositories;
using LIN.Cloud.Caddy.Services.Implementations;
using Moq;
using Xunit;

namespace LIN.Cloud.Caddy.Test;

public class ApiKeyServiceTests
{
    private readonly Mock<IApiKeyRepository> _repositoryMock;
    private readonly ApiKeyService _apiKeyService;

    public ApiKeyServiceTests()
    {
        _repositoryMock = new Mock<IApiKeyRepository>();
        _apiKeyService = new ApiKeyService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GenerateKey_CreatesAndReturnsKey()
    {
        // Arrange
        var description = "Test Key";
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<ApiKeyEntity>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _apiKeyService.GenerateKey(description);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(description, result.Description);
        Assert.False(string.IsNullOrEmpty(result.Key));
        Assert.True(result.IsActive);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ApiKeyEntity>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeactivateKey_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "existing-key";
        var entity = new ApiKeyEntity { Key = key, IsActive = true };
        _repositoryMock.Setup(r => r.GetByKeyAsync(key)).ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _apiKeyService.DeactivateKey(key);

        // Assert
        Assert.True(result);
        Assert.False(entity.IsActive);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeactivateKey_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var key = "non-existing-key";
        _repositoryMock.Setup(r => r.GetByKeyAsync(key)).ReturnsAsync((ApiKeyEntity?)null);

        // Act
        var result = await _apiKeyService.DeactivateKey(key);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
