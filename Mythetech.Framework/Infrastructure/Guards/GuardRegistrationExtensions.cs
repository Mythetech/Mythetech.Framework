using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mythetech.Framework.Infrastructure.Guards;

/// <summary>
/// Extension methods for registering JS guard services.
/// </summary>
public static class GuardRegistrationExtensions
{
    /// <summary>
    /// Adds the JS guard service for gating component rendering
    /// until JavaScript dependencies are available.
    /// </summary>
    public static IServiceCollection AddJsGuards(this IServiceCollection services)
    {
        services.TryAddSingleton<IJsGuardService, JsGuardService>();
        return services;
    }
}
