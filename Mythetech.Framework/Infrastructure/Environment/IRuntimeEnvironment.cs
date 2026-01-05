namespace Mythetech.Framework.Infrastructure.Environment;

/// <summary>
/// Provides runtime environment information for cross-platform Blazor applications.
/// This abstraction exists because Microsoft doesn't provide a common environment interface
/// across Blazor hosting models (Desktop, WebAssembly, Server).
/// </summary>
public interface IRuntimeEnvironment
{
    /// <summary>
    /// The environment name (e.g., "Development", "Production", "Staging").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The application version.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// The base address/URL for the application.
    /// </summary>
    string BaseAddress { get; }

    /// <summary>
    /// The hosting platform (Desktop or WebAssembly).
    /// </summary>
    Plugins.Platform Platform { get; }
}

/// <summary>
/// Extension methods for <see cref="IRuntimeEnvironment"/>.
/// </summary>
public static class IRuntimeEnvironmentExtensions
{
    /// <summary>
    /// Checks if the current environment matches the specified environment name.
    /// </summary>
    /// <param name="runtime">The runtime environment</param>
    /// <param name="environment">The environment name to check</param>
    /// <returns>True if the environment matches, false otherwise</returns>
    public static bool IsEnvironment(this IRuntimeEnvironment runtime, string environment)
        => string.Equals(environment, runtime.Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the current environment is Development.
    /// </summary>
    /// <param name="runtime">The runtime environment</param>
    /// <returns>True if running in Development, false otherwise</returns>
    public static bool IsDevelopment(this IRuntimeEnvironment runtime)
        => runtime.IsEnvironment("Development");

    /// <summary>
    /// Checks if the current environment is Production.
    /// </summary>
    /// <param name="runtime">The runtime environment</param>
    /// <returns>True if running in Production, false otherwise</returns>
    public static bool IsProduction(this IRuntimeEnvironment runtime)
        => runtime.IsEnvironment("Production");

    /// <summary>
    /// Checks if the current environment is Staging.
    /// </summary>
    /// <param name="runtime">The runtime environment</param>
    /// <returns>True if running in Staging, false otherwise</returns>
    public static bool IsStaging(this IRuntimeEnvironment runtime)
        => runtime.IsEnvironment("Staging");
}
