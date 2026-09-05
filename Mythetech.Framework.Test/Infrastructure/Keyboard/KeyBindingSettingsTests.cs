using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Keyboard;

public class KeyBindingSettingsTests
{
    [Fact(DisplayName = "Settings id is stable and overrides start empty")]
    public void Has_stable_id_and_empty_overrides()
    {
        var settings = new KeyBindingSettings(Options.Create(new KeyBindingSettingsOptions()));

        settings.SettingsId.ShouldBe("KeyBindings");
        settings.Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Display metadata comes from the options so each app can supply its own icon")]
    public void Display_metadata_comes_from_options()
    {
        var settings = new KeyBindingSettings(Options.Create(new KeyBindingSettingsOptions
        {
            DisplayName = "Keyboard",
            Icon = "custom-icon",
            Order = 12,
        }));

        settings.DisplayName.ShouldBe("Keyboard");
        settings.Icon.ShouldBe("custom-icon");
        settings.Order.ShouldBe(12);
    }

    [Fact(DisplayName = "AddKeyBindings alone registers the settings instance but not the settings section")]
    public void AddKeyBindings_registers_instance_only()
    {
        var services = new ServiceCollection();
        services.AddKeyBindings(b => b.Add("a", "A", null, (bus, ct) => Task.CompletedTask));

        var provider = services.BuildServiceProvider();

        provider.GetService<KeyBindingSettings>().ShouldNotBeNull();
        provider.GetRequiredService<IOptions<SettingsRegistrationOptions>>()
            .Value.DiscoveredSettingsTypes.ShouldNotContain(typeof(KeyBindingSettings));
    }

    [Fact(DisplayName = "AddKeyBindingSettings registers the domain for settings discovery")]
    public void AddKeyBindingSettings_registers_for_discovery()
    {
        var services = new ServiceCollection();
        services.AddKeyBindingSettings(o => o.DisplayName = "Shortcuts");

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<SettingsRegistrationOptions>>()
            .Value.DiscoveredSettingsTypes.ShouldContain(typeof(KeyBindingSettings));
        provider.GetRequiredService<KeyBindingSettings>().DisplayName.ShouldBe("Shortcuts");
    }

    [Fact(DisplayName = "AddKeyBindings alone resolves the service and registry")]
    public void AddKeyBindings_alone_resolves_the_subsystem()
    {
        var provider = BuildContainer(services =>
            services.AddKeyBindings(b => b.Add("a", "A", null, (bus, ct) => Task.CompletedTask)));

        provider.GetRequiredService<IKeyBindingService>().ShouldNotBeNull();
        provider.GetRequiredService<IKeyBindingRegistry>().GetAll().Count.ShouldBe(1);
    }

    [Fact(DisplayName = "AddKeyBindingSettings alone resolves the service and registry so the settings section can render")]
    public void AddKeyBindingSettings_alone_resolves_the_subsystem()
    {
        var provider = BuildContainer(services => services.AddKeyBindingSettings());

        provider.GetRequiredService<IKeyBindingService>().ShouldNotBeNull();
        provider.GetRequiredService<IKeyBindingRegistry>().GetAll().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Bindings then settings resolves the subsystem and keeps the declared definitions")]
    public void Bindings_then_settings_resolves_the_subsystem()
    {
        var provider = BuildContainer(services =>
        {
            services.AddKeyBindings(b => b.Add("a", "A", null, (bus, ct) => Task.CompletedTask));
            services.AddKeyBindingSettings();
        });

        provider.GetRequiredService<IKeyBindingService>().ShouldNotBeNull();
        provider.GetRequiredService<IKeyBindingRegistry>().GetAll().Count.ShouldBe(1);
        provider.GetRequiredService<IOptions<SettingsRegistrationOptions>>()
            .Value.DiscoveredSettingsTypes.ShouldContain(typeof(KeyBindingSettings));
    }

    [Fact(DisplayName = "Settings then bindings resolves the subsystem and keeps the declared definitions")]
    public void Settings_then_bindings_resolves_the_subsystem()
    {
        var provider = BuildContainer(services =>
        {
            services.AddKeyBindingSettings();
            services.AddKeyBindings(b => b.Add("a", "A", null, (bus, ct) => Task.CompletedTask));
        });

        provider.GetRequiredService<IKeyBindingService>().ShouldNotBeNull();
        provider.GetRequiredService<IKeyBindingRegistry>().GetAll().Count.ShouldBe(1);
        provider.GetRequiredService<IOptions<SettingsRegistrationOptions>>()
            .Value.DiscoveredSettingsTypes.ShouldContain(typeof(KeyBindingSettings));
    }

    [Fact(DisplayName = "Calling both registrations keeps a single shared service and settings instance")]
    public void Both_registrations_stay_idempotent()
    {
        var provider = BuildContainer(services =>
        {
            services.AddKeyBindings(b => b.Add("a", "A", null, (bus, ct) => Task.CompletedTask));
            services.AddKeyBindingSettings();
        });

        provider.GetRequiredService<IKeyBindingService>()
            .ShouldBeSameAs(provider.GetRequiredService<IKeyBindingService>());
        provider.GetRequiredService<KeyBindingSettings>()
            .ShouldBeSameAs(provider.GetRequiredService<KeyBindingSettings>());
    }

    private static ServiceProvider BuildContainer(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IMessageBus>());
        services.AddSettingsFramework();
        configure(services);

        return services.BuildServiceProvider();
    }
}
