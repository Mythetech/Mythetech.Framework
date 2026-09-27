using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Smoke;
using SampleHost.Shared.Settings;

namespace SampleHost.Desktop;

/// <summary>
/// An app-level smoke check: the sample's own settings were discovered and registered.
/// </summary>
public sealed class SampleSettingsSmokeCheck(ISettingsProvider settingsProvider) : ISmokeCheck
{
    public string Name => "sample/settings";

    public Task RunAsync(CancellationToken cancellationToken)
    {
        if (settingsProvider.GetSettings<SampleAppSettings>() is null)
            throw new InvalidOperationException("SampleAppSettings is not registered");

        return Task.CompletedTask;
    }
}
