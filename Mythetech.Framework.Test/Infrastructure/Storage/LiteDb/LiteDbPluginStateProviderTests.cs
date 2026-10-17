using LiteDB;
using Mythetech.Framework.Desktop.Storage.LiteDb;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.LiteDb;

public class LiteDbPluginStateProviderTests : IDisposable
{
    private readonly ManualResetEventSlim _callReturned = new();
    private bool? _readBeforeCallReturned;

    [Fact(DisplayName = "LoadDisabledPluginsAsync reads the database after the call returns")]
    public async Task LoadDisabledPluginsAsync_ReadsAfterTheCallReturns()
    {
        var collection = Substitute.For<ILiteCollection<PluginStateEntry>>();
        collection.FindById(Arg.Any<BsonValue>()).Returns(new PluginStateEntry { DisabledPluginsJson = """["plugin.a"]""" });
        var database = Substitute.For<ILiteDatabase>();
        database.GetCollection<PluginStateEntry>(Arg.Any<string>(), Arg.Any<BsonAutoId>()).Returns(_ =>
        {
            _readBeforeCallReturned = !_callReturned.Wait(TimeSpan.FromSeconds(2));
            return collection;
        });
        var provider = new LiteDbPluginStateProvider(database);

        var load = provider.LoadDisabledPluginsAsync();
        _callReturned.Set();
        var disabled = await load;

        _readBeforeCallReturned.ShouldBe(false);
        disabled.ShouldBe(new[] { "plugin.a" });
    }

    [Fact(DisplayName = "SaveDisabledPluginsAsync keeps the set from the last call when saves overlap")]
    public async Task SaveDisabledPluginsAsync_OverlappingSaves_KeepLastSet()
    {
        using var database = new LiteDatabase(new MemoryStream());
        var provider = new LiteDbPluginStateProvider(database);

        var first = provider.SaveDisabledPluginsAsync(new HashSet<string> { "plugin.a" });
        first.IsCompleted.ShouldBeTrue();
        var second = provider.SaveDisabledPluginsAsync(new HashSet<string> { "plugin.a", "plugin.b" });
        await Task.WhenAll(first, second);

        (await provider.LoadDisabledPluginsAsync()).ShouldBe(new[] { "plugin.a", "plugin.b" }, ignoreOrder: true);
    }

    public void Dispose() => _callReturned.Dispose();
}
