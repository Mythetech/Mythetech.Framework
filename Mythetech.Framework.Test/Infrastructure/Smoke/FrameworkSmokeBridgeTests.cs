using Hermes.Blazor.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Desktop.Smoke;
using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Smoke;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Smoke;

public class FrameworkSmokeBridgeTests
{
    private readonly IHermesSmokeSession _session = Substitute.For<IHermesSmokeSession>();

    [Fact(DisplayName = "Outside a smoke run only a disabled context is registered")]
    public void Outside_A_Smoke_Run_Registers_Only_The_Context()
    {
        var services = new ServiceCollection();

        services.AddFrameworkSmokeBridge(smokeRun: false);

        services.BuildServiceProvider().GetRequiredService<ISmokeTestContext>().IsEnabled.ShouldBeFalse();
        services.ShouldNotContain(d => d.ServiceType == typeof(IHermesSmokeCheckSource));
        services.ShouldNotContain(d => d.ServiceType == typeof(IMessagePipe<ApplicationReady>));
    }

    [Fact(DisplayName = "The smoke context is enabled whether AddSmokeChecks runs before or after the bridge")]
    public void Context_Is_Enabled_In_Either_Registration_Order()
    {
        var checksFirst = new ServiceCollection();
        checksFirst.AddSmokeChecks();
        checksFirst.AddFrameworkSmokeBridge(smokeRun: true);

        var bridgeFirst = new ServiceCollection();
        bridgeFirst.AddFrameworkSmokeBridge(smokeRun: true);
        bridgeFirst.AddSmokeChecks();

        checksFirst.BuildServiceProvider().GetRequiredService<ISmokeTestContext>().IsEnabled.ShouldBeTrue();
        bridgeFirst.BuildServiceProvider().GetRequiredService<ISmokeTestContext>().IsEnabled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Hands Hermes the general checks first, then the app's checks")]
    public void Check_Source_Returns_General_Then_App_Checks()
    {
        var provider = BuildSmokeProvider(services =>
        {
            services.AddSingleton(Substitute.For<ISettingsStorage>());
            services.AddSmokeChecks().WithSmokeCheck<AppCheck>();
        });

        var checks = ResolveHermesChecks(provider);

        checks.Select(c => c.Name).ShouldBe(["framework/storage", "sample/app-check"]);
        checks[1].Timeout.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact(DisplayName = "Adapted checks run the app's check")]
    public async Task Adapted_Checks_Run_The_App_Check()
    {
        var provider = BuildSmokeProvider(services => services.AddSmokeChecks().WithSmokeCheck<AppCheck>());
        using var scope = provider.CreateScope();
        var check = ResolveHermesChecks(scope.ServiceProvider).Single();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => check.RunAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("app check ran");
    }

    [Fact(DisplayName = "App-ready is released at check resolution when the app has no initialization host")]
    public void Releases_App_Ready_Without_An_Initialization_Host()
    {
        var provider = BuildSmokeProvider(services => services.AddSmokeChecks());

        ResolveHermesChecks(provider);

        _session.Received(1).CompleteGate(FrameworkSmokeBridge.AppReadyGate);
    }

    [Fact(DisplayName = "App-ready waits for ApplicationReady when the app has an initialization host and a bus")]
    public void Waits_For_ApplicationReady_With_An_Initialization_Host()
    {
        var provider = BuildSmokeProvider(services =>
        {
            services.AddMessageBus();
            services.AddAsyncInitialization();
            services.AddSmokeChecks();
        });

        ResolveHermesChecks(provider);

        _session.DidNotReceive().CompleteGate(Arg.Any<string>());
    }

    [Fact(DisplayName = "App-ready is released at check resolution when there is no bus to publish ApplicationReady on")]
    public void Releases_App_Ready_Without_A_Message_Bus()
    {
        var provider = BuildSmokeProvider(services =>
        {
            services.AddAsyncInitialization();
            services.AddSmokeChecks();
        });

        ResolveHermesChecks(provider);

        _session.Received(1).CompleteGate(FrameworkSmokeBridge.AppReadyGate);
    }

    [Fact(DisplayName = "Running initialization completes app-ready through the message bus")]
    public async Task Initialization_Completes_App_Ready()
    {
        var provider = BuildSmokeProvider(services =>
        {
            services.AddMessageBus();
            services.AddAsyncInitialization();
        });

        await provider.GetRequiredService<IAsyncInitializationHost>().InitializeAsync(TestContext.Current.CancellationToken);

        _session.Received(1).CompleteGate(FrameworkSmokeBridge.AppReadyGate);
    }

    [Fact(DisplayName = "An app publishing ApplicationReady itself completes app-ready")]
    public async Task Published_ApplicationReady_Completes_App_Ready()
    {
        var provider = BuildSmokeProvider(services => services.AddMessageBus());

        await provider.GetRequiredService<IMessageBus>().PublishAsync(new ApplicationReady([]));

        _session.Received(1).CompleteGate(FrameworkSmokeBridge.AppReadyGate);
    }

    private ServiceProvider BuildSmokeProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_session);
        configure(services);
        services.AddFrameworkSmokeBridge(smokeRun: true);
        return services.BuildServiceProvider();
    }

    private static List<IHermesSmokeCheck> ResolveHermesChecks(IServiceProvider scopedServices) =>
        scopedServices.GetServices<IHermesSmokeCheckSource>()
            .SelectMany(source => source.GetChecks(scopedServices))
            .ToList();

    private static List<IHermesSmokeCheck> ResolveHermesChecks(ServiceProvider provider)
    {
        // Only for tests that look at the checks without running them, so the scope can end here.
        using var scope = provider.CreateScope();
        return ResolveHermesChecks(scope.ServiceProvider);
    }

    private sealed class AppCheck : ISmokeCheck
    {
        public string Name => "sample/app-check";

        public TimeSpan Timeout => TimeSpan.FromSeconds(3);

        public Task RunAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("app check ran");
    }
}
