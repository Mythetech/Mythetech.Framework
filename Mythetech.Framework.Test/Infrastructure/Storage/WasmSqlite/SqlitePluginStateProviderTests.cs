using Mythetech.Framework.WebAssembly.Storage.Sqlite;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.WasmSqlite;

[Trait("Category", "WasmIntegration")]
public class SqlitePluginStateProviderTests
{
    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Load_WhenEmpty_ReturnsEmptySet()
    {
        var provider = new SqlitePluginStateProvider("state_test.db");

        var result = await provider.LoadDisabledPluginsAsync();

        result.ShouldBeEmpty();
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task SaveAndLoad_RoundTrips()
    {
        var provider = new SqlitePluginStateProvider("state_test.db");
        var disabled = new HashSet<string> { "plugin.a", "plugin.b" };

        await provider.SaveDisabledPluginsAsync(disabled);
        var result = await provider.LoadDisabledPluginsAsync();

        result.Count.ShouldBe(2);
        result.ShouldContain("plugin.a");
        result.ShouldContain("plugin.b");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Save_OverwritesPrevious()
    {
        var provider = new SqlitePluginStateProvider("state_test.db");

        await provider.SaveDisabledPluginsAsync(new HashSet<string> { "plugin.a", "plugin.b" });
        await provider.SaveDisabledPluginsAsync(new HashSet<string> { "plugin.c" });

        var result = await provider.LoadDisabledPluginsAsync();
        result.Count.ShouldBe(1);
        result.ShouldContain("plugin.c");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Save_EmptySet_ClearsAll()
    {
        var provider = new SqlitePluginStateProvider("state_test.db");

        await provider.SaveDisabledPluginsAsync(new HashSet<string> { "plugin.a" });
        await provider.SaveDisabledPluginsAsync(new HashSet<string>());

        var result = await provider.LoadDisabledPluginsAsync();
        result.ShouldBeEmpty();
    }
}
