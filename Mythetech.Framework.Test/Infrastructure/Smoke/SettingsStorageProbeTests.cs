using Mythetech.Framework.Desktop.Storage.LiteDb;
using Mythetech.Framework.Desktop.Storage.Sqlite;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Smoke;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Smoke;

public class SettingsStorageProbeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"smoke_storage_{Guid.NewGuid()}");

    public SettingsStorageProbeTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public static TheoryData<string> Storages => ["litedb", "sqlite"];

    [Theory(DisplayName = "framework/storage passes for a settings store that opens")]
    [MemberData(nameof(Storages))]
    public async Task Passes_For_A_Store_That_Opens(string kind)
    {
        var storage = Create(kind, Path.Combine(_directory, $"settings.{kind}"));
        await storage.SaveSettingsAsync("privacy", "{}");

        await new SettingsStorageSmokeCheck(storage).RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory(DisplayName = "framework/storage fails for a settings store that cannot open, which loading alone hides")]
    [MemberData(nameof(Storages))]
    public async Task Fails_For_A_Store_That_Cannot_Open(string kind)
    {
        var storage = Create(kind, Path.Combine(_directory, "missing-directory", $"settings.{kind}"));
        (await storage.LoadAllSettingsAsync()).ShouldBeEmpty();

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => new SettingsStorageSmokeCheck(storage).RunAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldStartWith("The settings store could not be opened:");
        ex.InnerException.ShouldNotBeNull();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A store can still hold its file open; the temp directory is cleaned up by the OS later.
        }
    }

    private static ISettingsStorage Create(string kind, string path) => kind switch
    {
        "litedb" => new LiteDbSettingsStorage(path),
        _ => new SqliteSettingsStorage(path),
    };
}
