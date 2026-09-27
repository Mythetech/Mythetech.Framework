using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Smoke;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Smoke;

public class InitializationSmokeCheckTests
{
    [Fact(DisplayName = "Passes when every initialization hook succeeded")]
    public async Task Passes_When_Every_Hook_Succeeded()
    {
        var host = Host(initialized: true, new InitializationHookResult("Settings", 100, TimeSpan.Zero, null));

        await new InitializationSmokeCheck(host).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "Fails when initialization has not finished")]
    public async Task Fails_When_Not_Initialized()
    {
        var host = Host(initialized: false);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => new InitializationSmokeCheck(host).RunAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldContain("has not finished");
    }

    [Fact(DisplayName = "Fails naming each failed hook and its exception")]
    public async Task Fails_Listing_Failed_Hooks()
    {
        var host = Host(initialized: true,
            new InitializationHookResult("Settings", 100, TimeSpan.Zero, new IOException("disk full")),
            new InitializationHookResult("Flags", 200, TimeSpan.Zero, null),
            new InitializationHookResult("Mcp", 300, TimeSpan.Zero, new InvalidOperationException("port taken")));

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => new InitializationSmokeCheck(host).RunAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("2 of 3 initialization hooks failed: Settings: IOException: disk full; Mcp: InvalidOperationException: port taken");
    }

    private static IAsyncInitializationHost Host(bool initialized, params InitializationHookResult[] results)
    {
        var host = Substitute.For<IAsyncInitializationHost>();
        host.IsInitialized.Returns(initialized);
        host.Results.Returns(results);
        return host;
    }
}

public class SettingsStorageSmokeCheckTests
{
    [Fact(DisplayName = "Passes when the settings store loads, and never writes to it")]
    public async Task Passes_And_Never_Writes()
    {
        var storage = Substitute.For<ISettingsStorage>();
        storage.LoadAllSettingsAsync().Returns(new Dictionary<string, string> { ["privacy"] = "{}" });

        await new SettingsStorageSmokeCheck(storage).RunAsync(TestContext.Current.CancellationToken);

        await storage.Received(1).LoadAllSettingsAsync();
        await storage.DidNotReceiveWithAnyArgs().SaveSettingsAsync(default!, default!);
    }

    [Fact(DisplayName = "Fails when the settings store cannot be read")]
    public async Task Fails_When_The_Store_Cannot_Be_Read()
    {
        var storage = Substitute.For<ISettingsStorage>();
        storage.LoadAllSettingsAsync().ThrowsAsync(new IOException("database is locked"));

        var ex = await Should.ThrowAsync<IOException>(
            () => new SettingsStorageSmokeCheck(storage).RunAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("database is locked");
    }
}

public class PluginLoadingSmokeCheckTests
{
    [Fact(DisplayName = "Passes straight away when plugins are already loaded")]
    public async Task Passes_When_Already_Loaded()
    {
        var state = new PluginState { PluginsLoaded = true };

        await new PluginLoadingSmokeCheck(state).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "Waits for plugin loading that completes after the check starts")]
    public async Task Waits_For_Loading_To_Complete()
    {
        var state = new PluginState();
        var check = new PluginLoadingSmokeCheck(state).RunAsync(TestContext.Current.CancellationToken);

        await Task.Delay(PluginLoadingSmokeCheck.PollInterval * 3, TestContext.Current.CancellationToken);
        check.IsCompleted.ShouldBeFalse();

        state.PluginsLoaded = true;
        await check.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "Gives up when cancelled before plugin loading completes")]
    public async Task Cancels_When_Loading_Never_Completes()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        await Should.ThrowAsync<OperationCanceledException>(
            () => new PluginLoadingSmokeCheck(new PluginState()).RunAsync(cts.Token));
    }
}
