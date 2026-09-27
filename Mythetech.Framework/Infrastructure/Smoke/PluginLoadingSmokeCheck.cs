using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.Infrastructure.Smoke;

/// <summary>
/// Passes once plugin loading has completed. Apps usually load plugins after the first render, so the
/// check waits for it within its timeout. Individual plugin load failures are logged as errors, which the
/// smoke run already treats as a failure.
/// </summary>
internal sealed class PluginLoadingSmokeCheck(PluginState pluginState) : ISmokeCheck
{
    // Polled rather than awaited through the message bus: a Framework type implementing IConsumer<T> would
    // be picked up by every app's assembly-scanned bus registration.
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    public string Name => "framework/plugins";

    public TimeSpan Timeout => TimeSpan.FromSeconds(30);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // Cancellation is left to propagate: the host reports it as this check timing out, or as the run's
        // budget running out, and tells the two apart.
        while (!pluginState.PluginsLoaded)
            await Task.Delay(PollInterval, cancellationToken);
    }
}
