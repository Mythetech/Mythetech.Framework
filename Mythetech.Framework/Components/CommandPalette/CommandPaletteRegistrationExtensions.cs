using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Components.Kbd;

namespace Mythetech.Framework.Components.CommandPalette;

/// <summary>
/// DI registration extensions for the command palette.
/// </summary>
public static class CommandPaletteRegistrationExtensions
{
    /// <summary>
    /// Registers the command palette service and a default <see cref="IPlatformDetector"/>.
    /// Consuming apps register their own <see cref="ICommandProvider"/> implementations
    /// via <see cref="AddCommandProvider{TProvider}"/>.
    /// </summary>
    public static IServiceCollection AddCommandPalette(this IServiceCollection services)
    {
        services.AddScoped<CommandPaletteService>();
        services.TryAddSingletonPlatformDetector();
        return services;
    }

    /// <summary>
    /// Registers a command provider that supplies commands to the palette.
    /// </summary>
    public static IServiceCollection AddCommandProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, ICommandProvider
    {
        services.AddScoped<ICommandProvider, TProvider>();
        return services;
    }

    private static void TryAddSingletonPlatformDetector(this IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(IPlatformDetector)))
        {
            return;
        }

        services.AddSingleton<IPlatformDetector, DefaultPlatformDetector>();
    }
}
