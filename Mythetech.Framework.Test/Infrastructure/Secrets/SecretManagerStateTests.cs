using Mythetech.Framework.Infrastructure.Secrets;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Secrets;

public class SecretManagerStateTests
{
    private readonly SecretManagerState _state;
    private ISecretManager _mockManager;

    public SecretManagerStateTests()
    {
        _state = new SecretManagerState();
        _mockManager = Substitute.For<ISecretManager>();
    }

    #region State-Manager Integration Tests

    [Fact(DisplayName = "RefreshSecretsAsync_WithManager_LoadsAndCachesSecrets")]
    public async Task RefreshSecretsAsync_WithManager_LoadsAndCachesSecrets()
    {
        // Arrange
        var secrets = new List<Secret>
        {
            new() { Key = "secret1", Value = "value1", Name = "Secret 1" },
            new() { Key = "secret2", Value = "value2", Name = "Secret 2" }
        };
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>()).Returns(secrets);
        _state.RegisterManager(_mockManager);

        // Act
        await _state.RefreshSecretsAsync();

        // Assert
        _state.Secrets.Count.ShouldBe(2);
        _state.Secrets.ShouldContain(s => s.Key == "secret1");
        _state.Secrets.ShouldContain(s => s.Key == "secret2");
    }

    [Fact(DisplayName = "RefreshSecretsAsync_NoManager_DoesNotThrow")]
    public async Task RefreshSecretsAsync_NoManager_DoesNotThrow()
    {
        // Arrange - no manager registered

        // Act & Assert
        await Should.NotThrowAsync(async () => await _state.RefreshSecretsAsync());
        _state.Secrets.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "GetSecretAsync_FromCache_ReturnsCachedSecret")]
    public async Task GetSecretAsync_FromCache_ReturnsCachedSecret()
    {
        // Arrange
        var secret = new Secret { Key = "secret1", Value = "value1", Name = "Secret 1" };
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>()).Returns(new[] { secret });
        _state.RegisterManager(_mockManager);
        await _state.RefreshSecretsAsync();

        // Act
        var result = await _state.GetSecretAsync("secret1");

        // Assert
        result.ShouldNotBeNull();
        result.Key.ShouldBe("secret1");
        result.Value.ShouldBe("value1");
        await _mockManager.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetSecretAsync_NotInCache_FetchesFromManager")]
    public async Task GetSecretAsync_NotInCache_FetchesFromManager()
    {
        // Arrange
        var secret = new Secret { Key = "secret1", Value = "value1", Name = "Secret 1" };
        _mockManager.GetSecretAsync("secret1", Arg.Any<CancellationToken>()).Returns(secret);
        _state.RegisterManager(_mockManager);

        // Act
        var result = await _state.GetSecretAsync("secret1");

        // Assert
        result.ShouldNotBeNull();
        result.Key.ShouldBe("secret1");
        _state.Secrets.ShouldContain(s => s.Key == "secret1");
    }

    [Fact(DisplayName = "GetSecretAsync_ManagerReturnsNull_ReturnsNull")]
    public async Task GetSecretAsync_ManagerReturnsNull_ReturnsNull()
    {
        // Arrange
        _mockManager.GetSecretAsync("nonexistent", Arg.Any<CancellationToken>()).Returns((Secret?)null);
        _state.RegisterManager(_mockManager);

        // Act
        var result = await _state.GetSecretAsync("nonexistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "SearchSecretsAsync_SearchesCachedSecrets")]
    public async Task SearchSecretsAsync_SearchesCachedSecrets()
    {
        // Arrange
        var secrets = new List<Secret>
        {
            new() { Key = "api-key", Value = "value1", Name = "API Key" },
            new() { Key = "db-password", Value = "value2", Name = "Database Password" }
        };
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>()).Returns(secrets);
        _state.RegisterManager(_mockManager);
        await _state.RefreshSecretsAsync();

        // Act
        var results = await _state.SearchSecretsAsync("api");

        // Assert
        results.Count().ShouldBe(1);
        results.ShouldContain(s => s.Key == "api-key");
    }

    [Fact(DisplayName = "SearchSecretsAsync_EmptyCache_ReturnsEmpty")]
    public async Task SearchSecretsAsync_EmptyCache_ReturnsEmpty()
    {
        // Arrange - empty cache

        // Act
        var results = await _state.SearchSecretsAsync("test");

        // Assert
        results.ShouldBeEmpty();
    }

    #endregion

    #region Failure Handling Tests

    [Fact(DisplayName = "RefreshSecretsAsync_ManagerThrowsException_HandlesGracefully")]
    public async Task RefreshSecretsAsync_ManagerThrowsException_HandlesGracefully()
    {
        // Arrange
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>()).Throws(new Exception("Connection failed"));
        _state.RegisterManager(_mockManager);

        // Act
        await _state.RefreshSecretsAsync();

        // Assert
        _state.Secrets.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "RefreshSecretsAsync_ManagerReturnsEmpty_UpdatesCacheToEmpty")]
    public async Task RefreshSecretsAsync_ManagerReturnsEmpty_UpdatesCacheToEmpty()
    {
        // Arrange
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Secret>());
        _state.RegisterManager(_mockManager);
        await _state.RefreshSecretsAsync();
        
        var initialSecrets = new List<Secret> { new() { Key = "secret1", Value = "value1" } };
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>()).Returns(initialSecrets);
        await _state.RefreshSecretsAsync();
        _state.Secrets.Count.ShouldBe(1);

        // Act
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Secret>());
        await _state.RefreshSecretsAsync();

        // Assert
        _state.Secrets.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "GetSecretAsync_ManagerThrowsException_ReturnsNull")]
    public async Task GetSecretAsync_ManagerThrowsException_ReturnsNull()
    {
        // Arrange
        _mockManager.GetSecretAsync("secret1", Arg.Any<CancellationToken>()).Throws(new Exception("Error"));
        _state.RegisterManager(_mockManager);

        // Act
        var result = await _state.GetSecretAsync("secret1");

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "IsAvailable_NoManager_ReturnsFalse")]
    public void IsAvailable_NoManager_ReturnsFalse()
    {
        // Arrange - no manager

        // Act
        var result = _state.IsAvailable;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsAvailable_ManagerRegistered_ReturnsTrue")]
    public void IsAvailable_ManagerRegistered_ReturnsTrue()
    {
        // Arrange
        _state.RegisterManager(_mockManager);

        // Act
        var result = _state.IsAvailable;

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region State Change Events

    [Fact(DisplayName = "RefreshSecretsAsync_RaisesStateChanged")]
    public async Task RefreshSecretsAsync_RaisesStateChanged()
    {
        // Arrange
        var eventRaised = false;
        _state.StateChanged += (_, _) => eventRaised = true;
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Secret>());
        _state.RegisterManager(_mockManager);

        // Act
        await _state.RefreshSecretsAsync();

        // Assert
        eventRaised.ShouldBeTrue();
    }

    [Fact(DisplayName = "RegisterManager_RaisesStateChanged")]
    public void RegisterManager_RaisesStateChanged()
    {
        // Arrange
        var eventRaised = false;
        _state.StateChanged += (_, _) => eventRaised = true;

        // Act
        _state.RegisterManager(_mockManager);

        // Assert
        eventRaised.ShouldBeTrue();
    }

    [Fact(DisplayName = "StateChanged_Subscribers_Notified")]
    public async Task StateChanged_Subscribers_Notified()
    {
        // Arrange
        var notificationCount = 0;
        _state.StateChanged += (_, _) => notificationCount++;
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Secret>());
        _state.RegisterManager(_mockManager);

        // Act
        await _state.RefreshSecretsAsync();

        // Assert
        notificationCount.ShouldBeGreaterThan(0);
    }

    #endregion
}

