using Hermes.Blazor.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Smoke;

namespace Mythetech.Framework.Desktop.Smoke;

/// <summary>
/// Connects the Framework's smoke checks and <see cref="ApplicationReady"/> to the Hermes smoke session.
/// </summary>
internal static class FrameworkSmokeBridge
{
    /// <summary>
    /// The Hermes gate the checks wait on: startup initialization has finished.
    /// </summary>
    public const string AppReadyGate = "app-ready";

    /// <summary>
    /// Registers the smoke context and, in a smoke run, the Hermes check source, the app-ready gate and the
    /// pipe that completes it. Outside a smoke run only the context is registered.
    /// </summary>
    public static IServiceCollection AddFrameworkSmokeBridge(this IServiceCollection services, bool smokeRun)
    {
        // Replace rather than TryAdd: AddSmokeChecks() may already have registered the disabled context.
        services.Replace(ServiceDescriptor.Singleton<ISmokeTestContext>(new DesktopSmokeTestContext(smokeRun)));

        if (!smokeRun)
            return services;

        services.AddHermesSmokeCheckSource<FrameworkSmokeCheckSource>();
        services.AddHermesSmokeGate(AppReadyGate);

        // A typed pipe, not an IConsumer: the bus resolves pipes from the container on first publish, while
        // consumers only run when the app scans their assembly, which apps do not do for this one.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessagePipe<ApplicationReady>, ApplicationReadyGatePipe>());

        return services;
    }
}

internal sealed class DesktopSmokeTestContext(bool isEnabled) : ISmokeTestContext
{
    public bool IsEnabled { get; } = isEnabled;
}

/// <summary>
/// Completes the app-ready gate when <see cref="ApplicationReady"/> is published, whatever the hooks'
/// outcome: <c>framework/initialization</c> is what reports failed hooks.
/// </summary>
internal sealed class ApplicationReadyGatePipe(IHermesSmokeSession session) : IMessagePipe<ApplicationReady>
{
    public Task<bool> ProcessAsync(ApplicationReady message, CancellationToken cancellationToken)
    {
        session.CompleteGate(FrameworkSmokeBridge.AppReadyGate);
        return Task.FromResult(true);
    }
}

/// <summary>
/// Hands the Framework's general checks and the app's <see cref="ISmokeCheck"/> registrations to Hermes.
/// </summary>
internal sealed class FrameworkSmokeCheckSource(IHermesSmokeSession session) : IHermesSmokeCheckSource
{
    public IEnumerable<IHermesSmokeCheck> GetChecks(IServiceProvider scopedServices)
    {
        // Hermes asks for checks at first render, before it waits on gates, so this is where apps that never
        // run the Framework's initialization host get app-ready released instead of timing out on it.
        var registered = scopedServices.GetRequiredService<IServiceProviderIsService>();
        if (!registered.IsService(typeof(IAsyncInitializationHost)))
            session.CompleteGate(FrameworkSmokeBridge.AppReadyGate);

        var general = scopedServices.GetService<FrameworkSmokeChecks>()?.GetChecks(scopedServices) ?? [];

        return general
            .Concat(scopedServices.GetServices<ISmokeCheck>())
            .Select(check => (IHermesSmokeCheck)new SmokeCheckAdapter(check))
            .ToList();
    }
}

internal sealed class SmokeCheckAdapter(ISmokeCheck check) : IHermesSmokeCheck
{
    public string Name => check.Name;

    public TimeSpan Timeout => check.Timeout;

    public Task RunAsync(CancellationToken cancellationToken) => check.RunAsync(cancellationToken);
}
