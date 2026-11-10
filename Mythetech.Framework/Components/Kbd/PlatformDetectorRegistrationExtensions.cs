using Microsoft.Extensions.DependencyInjection;

namespace Mythetech.Framework.Components.Kbd;

/// <summary>
/// DI registration extensions for platform detection.
/// </summary>
public static class PlatformDetectorRegistrationExtensions
{
    /// <summary>
    /// Registers <see cref="UserAgentPlatformDetector"/> as the <see cref="IPlatformDetector"/> for Blazor Server.
    /// It is scoped, so each circuit reports its own browser's platform. Desktop and WebAssembly hosts
    /// register their own detectors and don't call this.
    /// </summary>
    public static IServiceCollection AddUserAgentPlatformDetector(this IServiceCollection services)
    {
        services.AddScoped<UserAgentPlatformDetector>();
        services.AddScoped<IPlatformDetector>(provider => provider.GetRequiredService<UserAgentPlatformDetector>());
        return services;
    }
}
