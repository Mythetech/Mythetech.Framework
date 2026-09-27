using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Smoke;

/// <summary>
/// Passes when the settings store opens and its persisted settings can be read. It only reads: a local
/// smoke run uses the developer's real settings store, which must come out of the run untouched.
/// </summary>
internal sealed class SettingsStorageSmokeCheck(ISettingsStorage storage) : ISmokeCheck
{
    public string Name => "framework/storage";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await storage.LoadAllSettingsAsync().WaitAsync(cancellationToken);
    }
}
