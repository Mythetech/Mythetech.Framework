using Mythetech.Framework.Infrastructure.Environment;

namespace Mythetech.Framework.Desktop.Environment;

/// <summary>
/// Desktop implementation of <see cref="IRuntimeEnvironment"/>.
/// Configuration is passed via constructor since desktop apps don't have
/// the same environment discovery mechanisms as web applications.
/// </summary>
public class DesktopRuntimeEnvironment : IRuntimeEnvironment
{
    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public Version Version { get; }

    /// <inheritdoc/>
    public string BaseAddress { get; }

    /// <summary>
    /// Creates a new desktop runtime environment.
    /// </summary>
    /// <param name="name">The environment name (e.g., "Development", "Production")</param>
    /// <param name="version">The application version</param>
    /// <param name="baseAddress">The base address/URL for the application</param>
    public DesktopRuntimeEnvironment(string name, Version version, string baseAddress)
    {
        Name = name;
        Version = version;
        BaseAddress = baseAddress;
    }

    /// <summary>
    /// Creates a development environment with default settings.
    /// </summary>
    /// <param name="version">The application version</param>
    /// <param name="baseAddress">The base address (defaults to "app://")</param>
    /// <returns>A development runtime environment</returns>
    public static DesktopRuntimeEnvironment Development(Version? version = null, string baseAddress = "app://")
        => new("Development", version ?? new Version(1, 0, 0), baseAddress);

    /// <summary>
    /// Creates a production environment with default settings.
    /// </summary>
    /// <param name="version">The application version</param>
    /// <param name="baseAddress">The base address (defaults to "app://")</param>
    /// <returns>A production runtime environment</returns>
    public static DesktopRuntimeEnvironment Production(Version? version = null, string baseAddress = "app://")
        => new("Production", version ?? new Version(1, 0, 0), baseAddress);
}
