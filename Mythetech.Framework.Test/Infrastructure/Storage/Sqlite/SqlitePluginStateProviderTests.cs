using Mythetech.Framework.Desktop.Storage.Sqlite;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.Sqlite;

public class SqlitePluginStateProviderTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqlitePluginStateProvider _provider;

    public SqlitePluginStateProviderTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"state_test_{Guid.NewGuid()}.sqlite");
        _provider = new SqlitePluginStateProvider(_testDbPath);
    }

    [Fact(DisplayName = "LoadDisabledPluginsAsync returns empty set when no state exists")]
    public async Task Load_NoState_ReturnsEmptySet()
    {
        var disabled = await _provider.LoadDisabledPluginsAsync();
        disabled.ShouldBeEmpty();
    }

    [Fact(DisplayName = "SaveDisabledPluginsAsync persists disabled plugins")]
    public async Task Save_PersistsDisabledPlugins()
    {
        var plugins = new HashSet<string> { "plugin.a", "plugin.b" };

        await _provider.SaveDisabledPluginsAsync(plugins);

        var loaded = await _provider.LoadDisabledPluginsAsync();
        loaded.Count.ShouldBe(2);
        loaded.ShouldContain("plugin.a");
        loaded.ShouldContain("plugin.b");
    }

    [Fact(DisplayName = "SaveDisabledPluginsAsync overwrites previous state")]
    public async Task Save_OverwritesPreviousState()
    {
        await _provider.SaveDisabledPluginsAsync(new HashSet<string> { "plugin.a", "plugin.b" });
        await _provider.SaveDisabledPluginsAsync(new HashSet<string> { "plugin.c" });

        var loaded = await _provider.LoadDisabledPluginsAsync();
        loaded.Count.ShouldBe(1);
        loaded.ShouldContain("plugin.c");
    }

    [Fact(DisplayName = "SaveDisabledPluginsAsync with empty set clears state")]
    public async Task Save_EmptySet_ClearsState()
    {
        await _provider.SaveDisabledPluginsAsync(new HashSet<string> { "plugin.a" });
        await _provider.SaveDisabledPluginsAsync(new HashSet<string>());

        var loaded = await _provider.LoadDisabledPluginsAsync();
        loaded.ShouldBeEmpty();
    }

    [Fact(DisplayName = "State persists across provider instances")]
    public async Task State_PersistsAcrossInstances()
    {
        await _provider.SaveDisabledPluginsAsync(new HashSet<string> { "plugin.a", "plugin.b" });

        using var newProvider = new SqlitePluginStateProvider(_testDbPath);
        var loaded = await newProvider.LoadDisabledPluginsAsync();
        loaded.Count.ShouldBe(2);
        loaded.ShouldContain("plugin.a");
        loaded.ShouldContain("plugin.b");
    }

    public void Dispose()
    {
        _provider.Dispose();

        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }

        GC.SuppressFinalize(this);
    }
}
