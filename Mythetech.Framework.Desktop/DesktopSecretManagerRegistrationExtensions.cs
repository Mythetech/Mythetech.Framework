using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Desktop.Secrets;
using Mythetech.Framework.Infrastructure.Secrets;

namespace Mythetech.Framework.Desktop;

/// <summary>
/// Desktop-specific secret manager registration extensions
/// </summary>
public static class DesktopSecretManagerRegistrationExtensions
{
    /// <summary>
    /// Registers 1Password CLI secret manager for desktop applications
    /// </summary>
    public static IServiceCollection AddOnePasswordSecretManager(this IServiceCollection services)
    {
        services.AddSecretManager<OnePasswordCliSecretManager>();
        return services;
    }

    /// <summary>
    /// Registers native OS secret manager for desktop applications.
    /// Uses macOS Keychain, Windows Credential Manager, or Linux secret-tool.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceName">
    /// Optional service name for secret storage scope.
    /// Defaults to "mythetech/{entryAssemblyName}".
    /// </param>
    public static IServiceCollection AddNativeSecretManager(
        this IServiceCollection services,
        string? serviceName = null)
    {
        services.AddSecretManagerFramework();
        services.AddSingleton<ISecretManager>(new NativeSecretManager(serviceName));
        return services;
    }

    /// <summary>
    /// Registers all available desktop secret managers (1Password CLI and Native OS).
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="nativeServiceName">
    /// Optional service name for native secret manager.
    /// Defaults to "mythetech/{entryAssemblyName}".
    /// </param>
    public static IServiceCollection AddAllDesktopSecretManagers(
        this IServiceCollection services,
        string? nativeServiceName = null)
    {
        services.AddOnePasswordSecretManager();
        services.AddNativeSecretManager(nativeServiceName);
        return services;
    }
}

