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
}

