using Mythetech.Framework.Infrastructure.Secrets;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Secrets;

/// <summary>
/// Mock that implements both ISecretManager and ISecretSearcher for testing
/// </summary>
public interface ITestSecretManager : ISecretManager, ISecretSearcher
{
}

public class SecretManagerStateTests
{
    private readonly SecretManagerState _state;
    private ITestSecretManager _mockManager;

    public SecretManagerStateTests()
    {
        _state = new SecretManagerState();
        _mockManager = Substitute.For<ITestSecretManager>();
        _mockManager.Name.Returns("Test Manager");
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
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(secrets));
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
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(new[] { secret }));
        _state.RegisterManager(_mockManager);
        await _state.RefreshSecretsAsync();

        // Act
        var result = await _state.GetSecretAsync("secret1");

        // Assert
        result.Success.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Key.ShouldBe("secret1");
        result.Value.Value.ShouldBe("value1");
        await _mockManager.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetSecretAsync_NotInCache_FetchesFromManager")]
    public async Task GetSecretAsync_NotInCache_FetchesFromManager()
    {
        // Arrange
        var secret = new Secret { Key = "secret1", Value = "value1", Name = "Secret 1" };
        _mockManager.GetSecretAsync("secret1", Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<Secret>.Ok(secret));
        _state.RegisterManager(_mockManager);

        // Act
        var result = await _state.GetSecretAsync("secret1");

        // Assert
        result.Success.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Key.ShouldBe("secret1");
        _state.Secrets.ShouldContain(s => s.Key == "secret1");
    }

    [Fact(DisplayName = "GetSecretAsync_ManagerReturnsNotFound_ReturnsFailure")]
    public async Task GetSecretAsync_ManagerReturnsNotFound_ReturnsFailure()
    {
        // Arrange
        _mockManager.GetSecretAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<Secret>.Fail("Not found", SecretOperationErrorKind.NotFound));
        _state.RegisterManager(_mockManager);

        // Act
        var result = await _state.GetSecretAsync("nonexistent");

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.NotFound);
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
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(secrets));
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

    [Fact(DisplayName = "RefreshSecretsAsync_ManagerReturnsError_HandlesGracefully")]
    public async Task RefreshSecretsAsync_ManagerReturnsError_HandlesGracefully()
    {
        // Arrange
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Fail("Connection failed", SecretOperationErrorKind.ConnectionFailed));
        _state.RegisterManager(_mockManager);

        // Act
        var result = await _state.RefreshSecretsAsync();

        // Assert
        result.Success.ShouldBeFalse();
        _state.Secrets.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "RefreshSecretsAsync_ManagerReturnsEmpty_UpdatesCacheToEmpty")]
    public async Task RefreshSecretsAsync_ManagerReturnsEmpty_UpdatesCacheToEmpty()
    {
        // Arrange
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(Array.Empty<Secret>()));
        _state.RegisterManager(_mockManager);
        await _state.RefreshSecretsAsync();

        var initialSecrets = new List<Secret> { new() { Key = "secret1", Value = "value1" } };
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(initialSecrets));
        await _state.RefreshSecretsAsync();
        _state.Secrets.Count.ShouldBe(1);

        // Act
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(Array.Empty<Secret>()));
        await _state.RefreshSecretsAsync();

        // Assert
        _state.Secrets.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "GetSecretAsync_ManagerReturnsError_ReturnsFailure")]
    public async Task GetSecretAsync_ManagerReturnsError_ReturnsFailure()
    {
        // Arrange
        _mockManager.GetSecretAsync("secret1", Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<Secret>.Fail("Error", SecretOperationErrorKind.Unknown));
        _state.RegisterManager(_mockManager);

        // Act
        var result = await _state.GetSecretAsync("secret1");

        // Assert
        result.Success.ShouldBeFalse();
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
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(Array.Empty<Secret>()));
        _state.RegisterManager(_mockManager);
        eventRaised = false; // Reset after RegisterManager

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
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(Array.Empty<Secret>()));
        _state.RegisterManager(_mockManager);

        // Act
        await _state.RefreshSecretsAsync();

        // Assert
        notificationCount.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Multi-Manager Tests

    [Fact(DisplayName = "CanSearch_WithSearchableManager_ReturnsTrue")]
    public void CanSearch_WithSearchableManager_ReturnsTrue()
    {
        // Arrange
        _state.RegisterManager(_mockManager);

        // Act & Assert
        _state.CanSearch().ShouldBeTrue();
    }

    [Fact(DisplayName = "CanSearch_WithNonSearchableManager_ReturnsFalse")]
    public void CanSearch_WithNonSearchableManager_ReturnsFalse()
    {
        // Arrange
        var basicManager = Substitute.For<ISecretManager>();
        basicManager.Name.Returns("Basic Manager");
        _state.RegisterManager(basicManager);

        // Act & Assert
        _state.CanSearch().ShouldBeFalse();
    }

    [Fact(DisplayName = "AvailableManagers_MultipleRegistered_ContainsAll")]
    public void AvailableManagers_MultipleRegistered_ContainsAll()
    {
        // Arrange
        var manager2 = Substitute.For<ISecretManager>();
        manager2.Name.Returns("Manager 2");

        // Act
        _state.RegisterManager(_mockManager);
        _state.RegisterManager(manager2);

        // Assert
        _state.AvailableManagers.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "SetActiveManager_ByName_SwitchesManager")]
    public void SetActiveManager_ByName_SwitchesManager()
    {
        // Arrange
        var manager2 = Substitute.For<ISecretManager>();
        manager2.Name.Returns("Manager 2");
        _state.RegisterManager(_mockManager);
        _state.RegisterManager(manager2);

        // Act
        _state.SetActiveManager("Manager 2");

        // Assert
        _state.CurrentManager.ShouldBe(manager2);
    }

    #endregion

    #region Empty Value Cache Tests

    [Fact(DisplayName = "GetSecretAsync_CachedWithEmptyValue_FetchesFromManager")]
    public async Task GetSecretAsync_CachedWithEmptyValue_FetchesFromManager()
    {
        // Arrange - simulate list operation returning secret with empty value
        var secretFromList = new Secret { Key = "secret1", Value = "", Name = "Secret 1" };
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(new[] { secretFromList }));
        _state.RegisterManager(_mockManager);
        await _state.RefreshSecretsAsync();

        // Setup GetSecretAsync to return full secret with value
        var fullSecret = new Secret { Key = "secret1", Value = "actual-secret-value", Name = "Secret 1" };
        _mockManager.GetSecretAsync("secret1", Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<Secret>.Ok(fullSecret));

        // Act
        var result = await _state.GetSecretAsync("secret1");

        // Assert
        result.Success.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Value.ShouldBe("actual-secret-value");
        await _mockManager.Received(1).GetSecretAsync("secret1", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetSecretAsync_CachedWithEmptyValue_UpdatesCache")]
    public async Task GetSecretAsync_CachedWithEmptyValue_UpdatesCache()
    {
        // Arrange - simulate list operation returning secret with empty value
        var secretFromList = new Secret { Key = "secret1", Value = "", Name = "Secret 1" };
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(new[] { secretFromList }));
        _state.RegisterManager(_mockManager);
        await _state.RefreshSecretsAsync();

        // Setup GetSecretAsync to return full secret with value
        var fullSecret = new Secret { Key = "secret1", Value = "actual-secret-value", Name = "Secret 1" };
        _mockManager.GetSecretAsync("secret1", Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<Secret>.Ok(fullSecret));

        // Act
        await _state.GetSecretAsync("secret1");

        // Assert - cache should now have the full secret
        _state.Secrets.Count.ShouldBe(1);
        _state.Secrets[0].Value.ShouldBe("actual-secret-value");

        // Second call should use cached value and not call manager again
        _mockManager.ClearReceivedCalls();
        var secondResult = await _state.GetSecretAsync("secret1");
        secondResult.Value!.Value.ShouldBe("actual-secret-value");
        await _mockManager.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetSecretAsync_CachedWithValue_DoesNotFetch")]
    public async Task GetSecretAsync_CachedWithValue_DoesNotFetch()
    {
        // Arrange - cache has secret with actual value (not from list operation)
        var secretWithValue = new Secret { Key = "secret1", Value = "cached-value", Name = "Secret 1" };
        _mockManager.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(SecretOperationResult<IEnumerable<Secret>>.Ok(new[] { secretWithValue }));
        _state.RegisterManager(_mockManager);
        await _state.RefreshSecretsAsync();

        // Act
        var result = await _state.GetSecretAsync("secret1");

        // Assert - should return cached value without fetching
        result.Success.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Value.ShouldBe("cached-value");
        await _mockManager.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion
}

