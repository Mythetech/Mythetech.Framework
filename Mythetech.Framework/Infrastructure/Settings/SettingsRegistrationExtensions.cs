using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Components.Settings.Editors;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Consumers;

namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Options for tracking settings types discovered during service configuration.
/// </summary>
public class SettingsRegistrationOptions
{
    /// <summary>
    /// Types discovered via RegisterSettingsFromAssembly on IServiceCollection.
    /// </summary>
    public List<Type> DiscoveredSettingsTypes { get; } = new();
}

/// <summary>
/// Extension methods for registering settings framework services.
/// </summary>
public static class SettingsRegistrationExtensions
{
    /// <summary>
    /// Adds the core settings framework services.
    /// Call this before registering any settings models.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSettingsFramework(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsProvider, SettingsProvider>();

        // Register the editor registry with default editors and custom overrides
        services.AddSingleton<ISettingsEditorRegistry>(sp =>
        {
            var registry = new SettingsEditorRegistry();

            // Register default editors for built-in types
            registry.RegisterEditor(typeof(bool), typeof(BoolSettingEditor));
            registry.RegisterEditor(typeof(int), typeof(IntSettingEditor));
            registry.RegisterEditor(typeof(double), typeof(DoubleSettingEditor));
            registry.RegisterEditor(typeof(string), typeof(StringSettingEditor));
            registry.RegisterEditor(typeof(Enum), typeof(EnumSettingEditor));

            // Apply custom editor overrides from options
            var options = sp.GetService<IOptions<SettingsEditorOptions>>();
            if (options?.Value.CustomEditors != null)
            {
                foreach (var (dataType, editorType) in options.Value.CustomEditors)
                {
                    registry.RegisterEditor(dataType, editorType);
                }
            }

            return registry;
        });

        // Register consumers - they'll be discovered by AddMessageBus
        services.AddTransient<GenericSettingsModelConverter>();
        services.AddTransient<SettingsPersister>();

        // Also register consumers to the DI container
        var assembly = Assembly.GetExecutingAssembly();
        services.RegisterConsumers(assembly);

        return services;
    }

    /// <summary>
    /// Registers a custom settings editor for a specific data type.
    /// The custom editor will override any default editor for that type.
    /// </summary>
    /// <typeparam name="TDataType">The data type to provide an editor for.</typeparam>
    /// <typeparam name="TEditor">The Blazor component type that renders the editor.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterCustomSettingsEditor<TDataType, TEditor>(this IServiceCollection services)
        where TEditor : ComponentBase
    {
        services.Configure<SettingsEditorOptions>(options =>
            options.CustomEditors[typeof(TDataType)] = typeof(TEditor));
        return services;
    }

    /// <summary>
    /// Registers a custom settings editor for a specific data type.
    /// The custom editor will override any default editor for that type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="dataType">The data type to provide an editor for.</param>
    /// <param name="editorType">The Blazor component type that renders the editor.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterCustomSettingsEditor(
        this IServiceCollection services,
        Type dataType,
        Type editorType)
    {
        if (!typeof(ComponentBase).IsAssignableFrom(editorType))
        {
            throw new ArgumentException(
                $"Editor type {editorType.Name} must inherit from ComponentBase.",
                nameof(editorType));
        }

        services.Configure<SettingsEditorOptions>(options =>
            options.CustomEditors[dataType] = editorType);
        return services;
    }

    /// <summary>
    /// Registers a custom settings storage implementation.
    /// </summary>
    /// <typeparam name="TStorage">The storage implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSettingsStorage<TStorage>(this IServiceCollection services)
        where TStorage : class, ISettingsStorage
    {
        services.AddSingleton<ISettingsStorage, TStorage>();
        return services;
    }

    /// <summary>
    /// Registers a settings storage instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="storage">The storage instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSettingsStorage(this IServiceCollection services, ISettingsStorage storage)
    {
        services.AddSingleton(storage);
        return services;
    }

    /// <summary>
    /// Scans an assembly for SettingsBase implementations, registers them as singletons,
    /// and configures the SettingsProvider to auto-discover them.
    /// This allows consumers to inject settings models directly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for settings types.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterSettingsFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var settingsTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(SettingsBase).IsAssignableFrom(t))
            .Where(t => t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in settingsTypes)
        {
            services.AddSingleton(type);
        }

        services.Configure<SettingsRegistrationOptions>(options =>
        {
            foreach (var type in settingsTypes)
            {
                if (!options.DiscoveredSettingsTypes.Contains(type))
                    options.DiscoveredSettingsTypes.Add(type);
            }
        });

        return services;
    }

    /// <summary>
    /// Scans multiple assemblies for SettingsBase implementations, registers them as singletons,
    /// and configures the SettingsProvider to auto-discover them.
    /// This allows consumers to inject settings models directly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for settings types.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterSettingsFromAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            services.RegisterSettingsFromAssembly(assembly);
        }
        return services;
    }

    /// <summary>
    /// Registers a settings model with the provider.
    /// Call after building the service provider.
    /// </summary>
    /// <typeparam name="T">The settings type to register.</typeparam>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    [Obsolete("Use IServiceCollection.RegisterSettingsFromAssembly() instead for DI-injectable settings.")]
    public static IServiceProvider RegisterSettings<T>(this IServiceProvider serviceProvider)
        where T : SettingsBase, new()
    {
        var provider = serviceProvider.GetRequiredService<ISettingsProvider>();
        provider.RegisterSettings(new T());
        return serviceProvider;
    }

    /// <summary>
    /// Registers a settings model instance with the provider.
    /// Call after building the service provider.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <param name="settings">The settings instance to register.</param>
    /// <returns>The service provider for chaining.</returns>
    [Obsolete("Use IServiceCollection.RegisterSettingsFromAssembly() instead for DI-injectable settings.")]
    public static IServiceProvider RegisterSettings(this IServiceProvider serviceProvider, SettingsBase settings)
    {
        var provider = serviceProvider.GetRequiredService<ISettingsProvider>();
        provider.RegisterSettings(settings);
        return serviceProvider;
    }

    /// <summary>
    /// Scans an assembly for SettingsBase implementations and registers them.
    /// Only types with a parameterless constructor are registered.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <param name="assembly">The assembly to scan for settings types.</param>
    /// <returns>The service provider for chaining.</returns>
    [Obsolete("Use IServiceCollection.RegisterSettingsFromAssembly() instead for DI-injectable settings.")]
    public static IServiceProvider RegisterSettingsFromAssembly(this IServiceProvider serviceProvider, Assembly assembly)
    {
        var provider = serviceProvider.GetRequiredService<ISettingsProvider>();
        var settingsTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(SettingsBase).IsAssignableFrom(t))
            .Where(t => t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in settingsTypes)
        {
            var instance = (SettingsBase)Activator.CreateInstance(type)!;
            provider.RegisterSettings(instance);
        }

        return serviceProvider;
    }

    /// <summary>
    /// Scans multiple assemblies for SettingsBase implementations and registers them.
    /// Only types with a parameterless constructor are registered.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <param name="assemblies">The assemblies to scan for settings types.</param>
    /// <returns>The service provider for chaining.</returns>
    [Obsolete("Use IServiceCollection.RegisterSettingsFromAssemblies() instead for DI-injectable settings.")]
    public static IServiceProvider RegisterSettingsFromAssemblies(
        this IServiceProvider serviceProvider,
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            serviceProvider.RegisterSettingsFromAssembly(assembly);
        }

        return serviceProvider;
    }

    /// <summary>
    /// Loads persisted settings into registered models.
    /// Call after all settings are registered.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    public static async Task<IServiceProvider> LoadPersistedSettingsAsync(this IServiceProvider serviceProvider)
    {
        var storage = serviceProvider.GetService<ISettingsStorage>();
        if (storage == null)
        {
            // No storage configured - skip loading
            return serviceProvider;
        }

        var provider = serviceProvider.GetRequiredService<ISettingsProvider>();
        var data = await storage.LoadAllSettingsAsync();
        await provider.ApplyPersistedSettingsAsync(data);

        return serviceProvider;
    }

    /// <summary>
    /// Activates the settings framework by registering consumers to the message bus.
    /// Call after UseMessageBus().
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    public static IServiceProvider UseSettingsFramework(this IServiceProvider serviceProvider)
    {
        var bus = serviceProvider.GetRequiredService<IMessageBus>();
        var assembly = typeof(SettingsRegistrationExtensions).Assembly;
        bus.RegisterConsumersToBus(assembly);
        return serviceProvider;
    }
}
