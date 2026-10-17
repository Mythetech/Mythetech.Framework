using LiteDB;
using Mythetech.Framework.Desktop.Storage.LiteDb;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.LiteDb;

public class LiteDbSettingsStorageTests : IDisposable
{
    private readonly ManualResetEventSlim _callReturned = new();
    private bool? _readBeforeCallReturned;

    [Fact(DisplayName = "LoadAllSettingsAsync reads the database after the call returns")]
    public async Task LoadAllSettingsAsync_ReadsAfterTheCallReturns()
    {
        var collection = Substitute.For<ILiteCollection<SettingsStorageEntry>>();
        collection.FindAll().Returns([new SettingsStorageEntry { SettingsId = "app", JsonData = "{}" }]);
        var storage = new LiteDbSettingsStorage(DatabaseThatWaitsForTheCall(collection));

        var load = storage.LoadAllSettingsAsync();
        _callReturned.Set();
        var settings = await load;

        _readBeforeCallReturned.ShouldBe(false);
        settings["app"].ShouldBe("{}");
    }

    [Fact(DisplayName = "LoadSettingsAsync reads the database after the call returns")]
    public async Task LoadSettingsAsync_ReadsAfterTheCallReturns()
    {
        var collection = Substitute.For<ILiteCollection<SettingsStorageEntry>>();
        collection.FindById(Arg.Any<BsonValue>()).Returns(new SettingsStorageEntry { SettingsId = "app", JsonData = "{}" });
        var storage = new LiteDbSettingsStorage(DatabaseThatWaitsForTheCall(collection));

        var load = storage.LoadSettingsAsync("app");
        _callReturned.Set();
        var json = await load;

        _readBeforeCallReturned.ShouldBe(false);
        json.ShouldBe("{}");
    }

    [Fact(DisplayName = "ProbeAsync reads the database after the call returns")]
    public async Task ProbeAsync_ReadsAfterTheCallReturns()
    {
        var collection = Substitute.For<ILiteCollection<SettingsStorageEntry>>();
        ISettingsStorageProbe storage = new LiteDbSettingsStorage(DatabaseThatWaitsForTheCall(collection));

        var probe = storage.ProbeAsync(CancellationToken.None);
        _callReturned.Set();
        await probe;

        _readBeforeCallReturned.ShouldBe(false);
    }

    [Fact(DisplayName = "SaveSettingsAsync keeps the value from the last call when saves overlap")]
    public async Task SaveSettingsAsync_OverlappingSaves_KeepLastValue()
    {
        using var database = new LiteDatabase(new MemoryStream());
        var storage = new LiteDbSettingsStorage(database);

        var first = storage.SaveSettingsAsync("app", """{"theme":"dark"}""");
        first.IsCompleted.ShouldBeTrue();
        var second = storage.SaveSettingsAsync("app", """{"theme":"light"}""");
        await Task.WhenAll(first, second);

        (await storage.LoadSettingsAsync("app")).ShouldBe("""{"theme":"light"}""");
    }

    [Fact(DisplayName = "LoadAllSettingsAsync returns every saved settings entry")]
    public async Task LoadAllSettingsAsync_ReturnsSavedEntries()
    {
        using var database = new LiteDatabase(new MemoryStream());
        var storage = new LiteDbSettingsStorage(database);
        await storage.SaveSettingsAsync("app", """{"theme":"dark"}""");
        await storage.SaveSettingsAsync("editor", """{"fontSize":14}""");

        var settings = await storage.LoadAllSettingsAsync();

        settings.Count.ShouldBe(2);
        settings["editor"].ShouldBe("""{"fontSize":14}""");
    }

    private ILiteDatabase DatabaseThatWaitsForTheCall(ILiteCollection<SettingsStorageEntry> collection)
    {
        var database = Substitute.For<ILiteDatabase>();
        database.GetCollection<SettingsStorageEntry>(Arg.Any<string>(), Arg.Any<BsonAutoId>()).Returns(_ =>
        {
            _readBeforeCallReturned = !_callReturned.Wait(TimeSpan.FromSeconds(2));
            return collection;
        });
        return database;
    }

    public void Dispose() => _callReturned.Dispose();
}
