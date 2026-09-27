using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Smoke;

/// <summary>
/// Passes when the settings store opens and can be read. It only reads: a local smoke run uses the
/// developer's real settings store, which must come out of the run untouched.
/// </summary>
internal sealed class SettingsStorageSmokeCheck(ISettingsStorage storage) : ISmokeCheck
{
    public string Name => "framework/storage";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // The Framework's storages log and swallow their own failures, so loading through them would always
        // pass; their probe throws instead. Other storages are trusted to throw from LoadAllSettingsAsync.
        if (storage is ISettingsStorageProbe probe)
            await probe.ProbeAsync(cancellationToken);
        else
            await storage.LoadAllSettingsAsync().WaitAsync(cancellationToken);
    }
}
