using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Smoke;
using Mythetech.Framework.WebAssembly;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Smoke;

public class SmokeCheckRegistrationTests
{
    [Fact(DisplayName = "AddSmokeChecks registers a disabled smoke context")]
    public void AddSmokeChecks_Registers_A_Disabled_Context()
    {
        var services = new ServiceCollection();
        services.AddSmokeChecks();

        services.BuildServiceProvider().GetRequiredService<ISmokeTestContext>().IsEnabled.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddWebAssemblyServices registers a disabled smoke context")]
    public void WebAssembly_Registers_A_Disabled_Context()
    {
        var services = new ServiceCollection();
        services.AddWebAssemblyServices();

        services.BuildServiceProvider().GetRequiredService<ISmokeTestContext>().IsEnabled.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddSmokeChecks keeps a smoke context a host already registered")]
    public void AddSmokeChecks_Keeps_An_Existing_Context()
    {
        var hostContext = Substitute.For<ISmokeTestContext>();
        hostContext.IsEnabled.Returns(true);
        var services = new ServiceCollection();
        services.AddSingleton(hostContext);

        services.AddSmokeChecks();

        services.BuildServiceProvider().GetRequiredService<ISmokeTestContext>().ShouldBeSameAs(hostContext);
    }

    [Fact(DisplayName = "WithSmokeCheck registers each check once, as transient")]
    public void WithSmokeCheck_Registers_Each_Check_Once()
    {
        var services = new ServiceCollection();

        services.AddSmokeChecks()
            .WithSmokeCheck<FirstCheck>()
            .WithSmokeCheck<SecondCheck>()
            .WithSmokeCheck<FirstCheck>();

        var checks = services.Where(d => d.ServiceType == typeof(ISmokeCheck)).ToList();
        checks.Select(d => d.ImplementationType).ShouldBe([typeof(FirstCheck), typeof(SecondCheck)]);
        checks.ShouldAllBe(d => d.Lifetime == ServiceLifetime.Transient);
    }

    [Fact(DisplayName = "No general checks apply when the app uses none of the checked Framework parts")]
    public void No_General_Checks_Without_Framework_Parts()
    {
        var provider = BuildProvider(_ => { });

        GeneralCheckNames(provider).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Each general check applies only when its Framework part is registered")]
    public void General_Checks_Follow_Registered_Parts()
    {
        GeneralCheckNames(BuildProvider(s => s.AddSingleton(Substitute.For<IAsyncInitializationHost>())))
            .ShouldBe(["framework/initialization"]);
        GeneralCheckNames(BuildProvider(s => s.AddSingleton(Substitute.For<ISettingsStorage>())))
            .ShouldBe(["framework/storage"]);
        GeneralCheckNames(BuildProvider(s => s.AddSingleton(new PluginState())))
            .ShouldBe(["framework/plugins"]);
    }

    [Fact(DisplayName = "Framework parts registered after AddSmokeChecks still get their checks")]
    public void Registration_Order_Does_Not_Matter()
    {
        var services = new ServiceCollection();
        services.AddSmokeChecks();
        services.AddLogging();
        services.AddAsyncInitialization();

        var provider = services.BuildServiceProvider();

        GeneralCheckNames(provider).ShouldBe(["framework/initialization"]);
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSmokeChecks();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static List<string> GeneralCheckNames(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FrameworkSmokeChecks>()
            .GetChecks(scope.ServiceProvider)
            .Select(check => check.Name)
            .ToList();
    }

    private sealed class FirstCheck : ISmokeCheck
    {
        public string Name => "test/first";

        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class SecondCheck : ISmokeCheck
    {
        public string Name => "test/second";

        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
