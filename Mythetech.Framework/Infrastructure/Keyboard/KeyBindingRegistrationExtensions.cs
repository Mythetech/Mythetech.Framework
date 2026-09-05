using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// Definitions gathered from every <c>AddKeyBindings</c> call so that
/// multiple registration extensions compose instead of replacing each other.
/// </summary>
public sealed class KeyBindingRegistrationOptions
{
    /// <summary>Every declared definition, in registration order.</summary>
    public List<KeyBindingDefinition> Definitions { get; } = new();
}

/// <summary>
/// Extension methods for registering the key binding subsystem.
/// </summary>
public static class KeyBindingRegistrationExtensions
{
    /// <summary>
    /// Declares shortcut actions. Call this from the application's own
    /// registration extension. Safe to call more than once.
    ///
    /// <c>AddSettingsFramework()</c> must be called as well, in either order:
    /// <see cref="IKeyBindingService"/> depends on <c>ISettingsProvider</c> to
    /// persist the user's overrides and cannot be resolved without it.
    /// </summary>
    public static IServiceCollection AddKeyBindings(
        this IServiceCollection services,
        Action<KeyBindingBuilder> configure)
    {
        var builder = new KeyBindingBuilder();
        configure(builder);
        var definitions = builder.Build();

        services.Configure<KeyBindingRegistrationOptions>(options => options.Definitions.AddRange(definitions));
        AddKeyBindingServices(services);

        return services;
    }

    /// <summary>
    /// Adds the shortcuts section to the settings panel and persists user
    /// overrides. Registers the subsystem itself as well, so the section always
    /// has the services its editor injects even when <see cref="AddKeyBindings"/>
    /// is never called. Carries the same <c>AddSettingsFramework()</c>
    /// requirement described on <see cref="AddKeyBindings"/>.
    /// </summary>
    public static IServiceCollection AddKeyBindingSettings(
        this IServiceCollection services,
        Action<KeyBindingSettingsOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        AddKeyBindingServices(services);

        services.Configure<SettingsRegistrationOptions>(options =>
        {
            if (!options.DiscoveredSettingsTypes.Contains(typeof(KeyBindingSettings)))
            {
                options.DiscoveredSettingsTypes.Add(typeof(KeyBindingSettings));
            }
        });

        return services;
    }

    private static void AddKeyBindingServices(IServiceCollection services)
    {
        // Makes the registry's IOptions dependency explicit rather than relying on
        // some other Configure call having registered the options plumbing. Adds no
        // configure action, so an empty definition list stays empty.
        services.AddOptions<KeyBindingRegistrationOptions>();

        services.TryAddSingleton<IKeyBindingRegistry, KeyBindingRegistry>();
        services.TryAddSingleton<KeyBindingSettings>();
        services.TryAddSingleton<IKeyBindingService, KeyBindingService>();
    }
}
