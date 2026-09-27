using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Smoke;

/// <summary>
/// The general Framework checks. Registered by <see cref="SmokeCheckRegistrationExtensions.AddSmokeChecks"/>
/// and resolved by the desktop host bridge in a smoke run.
/// </summary>
internal sealed class FrameworkSmokeChecks
{
    /// <summary>
    /// Returns a check for each part of the Framework the app registered. Applicability is decided here,
    /// against the built container, so AddSmokeChecks() can be called before or after the parts it checks.
    /// </summary>
    public IEnumerable<ISmokeCheck> GetChecks(IServiceProvider scopedServices)
    {
        var registered = scopedServices.GetRequiredService<IServiceProviderIsService>();

        if (registered.IsService(typeof(IAsyncInitializationHost)))
            yield return new InitializationSmokeCheck(scopedServices.GetRequiredService<IAsyncInitializationHost>());

        if (registered.IsService(typeof(ISettingsStorage)))
            yield return new SettingsStorageSmokeCheck(scopedServices.GetRequiredService<ISettingsStorage>());

        if (registered.IsService(typeof(PluginState)))
            yield return new PluginLoadingSmokeCheck(scopedServices.GetRequiredService<PluginState>());
    }
}
