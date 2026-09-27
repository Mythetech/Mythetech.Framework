using Mythetech.Framework.Infrastructure.Initialization;

namespace Mythetech.Framework.Infrastructure.Smoke;

/// <summary>
/// Passes when every async initialization hook succeeded. Hooks log and swallow their own failures so the
/// app keeps starting, which is why a smoke run needs this check to see them.
/// </summary>
internal sealed class InitializationSmokeCheck(IAsyncInitializationHost host) : ISmokeCheck
{
    public string Name => "framework/initialization";

    public Task RunAsync(CancellationToken cancellationToken)
    {
        if (!host.IsInitialized)
            throw new InvalidOperationException("Async initialization has not finished");

        var failed = host.Results.Where(result => !result.Succeeded).ToList();
        if (failed.Count > 0)
        {
            var details = string.Join("; ", failed.Select(result => $"{result.Name}: {result.Error!.GetType().Name}: {result.Error.Message}"));
            throw new InvalidOperationException($"{failed.Count} of {host.Results.Count} initialization hooks failed: {details}");
        }

        return Task.CompletedTask;
    }
}
