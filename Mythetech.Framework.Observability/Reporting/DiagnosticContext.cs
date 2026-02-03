namespace Mythetech.Framework.Observability.Reporting;

/// <summary>
/// Diagnostic context information collected automatically with reports.
/// </summary>
public record DiagnosticContext
{
    /// <summary>
    /// Gets the timestamp when the context was collected.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the operating system name and version.
    /// </summary>
    public string? OperatingSystem { get; init; }

    /// <summary>
    /// Gets the .NET runtime version.
    /// </summary>
    public string? RuntimeVersion { get; init; }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string? AppVersion { get; init; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string? AppName { get; init; }

    /// <summary>
    /// Gets memory information.
    /// </summary>
    public MemoryInfo? Memory { get; init; }

    /// <summary>
    /// Gets information about loaded plugins.
    /// </summary>
    public IReadOnlyList<PluginInfo>? Plugins { get; init; }

    /// <summary>
    /// Gets the current correlation ID if available.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets additional custom data from registered providers.
    /// </summary>
    public IReadOnlyDictionary<string, object>? CustomData { get; init; }
}

/// <summary>
/// Memory usage information.
/// </summary>
public record MemoryInfo
{
    /// <summary>
    /// Gets the working set size in bytes.
    /// </summary>
    public long WorkingSetBytes { get; init; }

    /// <summary>
    /// Gets the GC total memory in bytes.
    /// </summary>
    public long GcTotalMemoryBytes { get; init; }

    /// <summary>
    /// Gets the GC heap size in bytes.
    /// </summary>
    public long GcHeapSizeBytes { get; init; }

    /// <summary>
    /// Gets the GC generation counts.
    /// </summary>
    public int[]? GcCollectionCounts { get; init; }
}

/// <summary>
/// Information about a loaded plugin.
/// </summary>
public record PluginInfo
{
    /// <summary>
    /// Gets the plugin identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the plugin name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets whether the plugin is enabled.
    /// </summary>
    public bool IsEnabled { get; init; }
}
