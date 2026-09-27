using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Initialization;

namespace SampleHost.Desktop;

/// <summary>
/// Stands in for an app's own startup work, so the sample's smoke run covers framework/initialization with a
/// real hook and the checks genuinely wait for it.
/// </summary>
public sealed class SampleStartupHook(ILogger<SampleStartupHook> logger) : IAsyncInitializationHook
{
    public string Name => "Sample startup";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        logger.LogInformation("Sample startup work finished");
    }
}
