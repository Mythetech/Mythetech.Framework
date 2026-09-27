using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Secrets;

/// <summary>
/// Wires the secret manager, message bus and settings framework the way a host does, with an in-memory
/// settings store standing in for disk, so the active-manager choice goes through the real save and load path.
/// </summary>
public class SecretManagerPersistenceTests
{
    private readonly ISecretManager _keychain = NamedManager("macOS Keychain");
    private readonly ISecretManager _onePassword = NamedManager("1Password CLI");
    private readonly InMemorySettingsStorage _storage = new();

    [Fact(DisplayName = "The chosen manager is active again after a restart once settings load")]
    public async Task ChosenManager_IsRestoredAfterRestart()
    {
        await using (var firstLaunch = Launch())
        {
            var state = firstLaunch.GetRequiredService<SecretManagerState>();
            state.CurrentManager.ShouldBe(_keychain);

            await state.SetActiveManagerAsync(_onePassword.Name);
        }

        await using var secondLaunch = Launch();
        var restored = secondLaunch.GetRequiredService<SecretManagerState>();
        restored.CurrentManager.ShouldBe(_keychain);

        await secondLaunch.LoadPersistedSettingsAsync();

        restored.CurrentManager.ShouldBe(_onePassword);
    }

    [Fact(DisplayName = "Settings loaded before the managers are registered still pick the saved manager")]
    public async Task SettingsLoadedBeforeRegistration_RestoresSavedManager()
    {
        _storage.Saved["SecretManager"] = """{"ActiveManagerName":"1Password CLI"}""";
        await using var services = Launch(registerManagers: false);

        await services.LoadPersistedSettingsAsync();
        services.UseSecretManager();

        services.GetRequiredService<SecretManagerState>().CurrentManager.ShouldBe(_onePassword);
    }

    [Fact(DisplayName = "A saved manager that is no longer registered falls back to the first manager")]
    public async Task SavedManagerNotRegistered_FallsBackToFirst()
    {
        _storage.Saved["SecretManager"] = """{"ActiveManagerName":"Retired Vault"}""";
        await using var services = Launch();

        await services.LoadPersistedSettingsAsync();

        services.GetRequiredService<SecretManagerState>().CurrentManager.ShouldBe(_keychain);
    }

    private ServiceProvider Launch(bool registerManagers = true)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMessageBus(Array.Empty<Assembly>());
        services.AddSettingsFramework();
        services.AddSettingsStorage(_storage);
        services.AddSecretManager(_keychain);
        services.AddSecretManager(_onePassword);

        var provider = services.BuildServiceProvider();
        provider.UseSettingsFramework();

        if (registerManagers)
        {
            provider.UseSecretManager();
        }

        return provider;
    }

    private static ISecretManager NamedManager(string name)
    {
        var manager = Substitute.For<ISecretManager>();
        manager.Name.Returns(name);
        return manager;
    }

    private sealed class InMemorySettingsStorage : ISettingsStorage
    {
        public Dictionary<string, string> Saved { get; } = new();

        public Task SaveSettingsAsync(string settingsId, string jsonData)
        {
            Saved[settingsId] = jsonData;
            return Task.CompletedTask;
        }

        public Task<string?> LoadSettingsAsync(string settingsId)
            => Task.FromResult(Saved.TryGetValue(settingsId, out var json) ? json : null);

        public Task<Dictionary<string, string>> LoadAllSettingsAsync()
            => Task.FromResult(new Dictionary<string, string>(Saved));
    }
}
