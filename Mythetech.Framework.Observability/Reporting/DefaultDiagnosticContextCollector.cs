using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Observability.Context;

namespace Mythetech.Framework.Observability.Reporting;

/// <summary>
/// Default implementation of diagnostic context collector.
/// Collects OS, runtime, memory, and custom provider data.
/// </summary>
public class DefaultDiagnosticContextCollector : IDiagnosticContextCollector
{
    private readonly IEnumerable<IDiagnosticContextProvider> _providers;
    private readonly IOperationContext? _operationContext;
    private readonly ILogger<DefaultDiagnosticContextCollector>? _logger;
    private readonly string? _appName;
    private readonly string? _appVersion;

    /// <summary>
    /// Creates a new DefaultDiagnosticContextCollector.
    /// </summary>
    /// <param name="providers">Registered diagnostic context providers.</param>
    /// <param name="operationContext">Optional operation context for correlation IDs.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="appName">Optional application name.</param>
    /// <param name="appVersion">Optional application version.</param>
    public DefaultDiagnosticContextCollector(
        IEnumerable<IDiagnosticContextProvider> providers,
        IOperationContext? operationContext = null,
        ILogger<DefaultDiagnosticContextCollector>? logger = null,
        string? appName = null,
        string? appVersion = null)
    {
        _providers = providers;
        _operationContext = operationContext;
        _logger = logger;
        _appName = appName;
        _appVersion = appVersion;
    }

    /// <inheritdoc />
    public async Task<DiagnosticContext> CollectAsync(CancellationToken ct = default)
    {
        var customData = new Dictionary<string, object>();

        foreach (var provider in _providers)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var data = await provider.CollectAsync(ct);
                customData[provider.Category] = data;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex, "Diagnostic context provider {Category} failed", provider.Category);
            }
        }

        return new DiagnosticContext
        {
            Timestamp = DateTime.UtcNow,
            OperatingSystem = GetOperatingSystem(),
            RuntimeVersion = RuntimeInformation.FrameworkDescription,
            AppName = _appName ?? GetEntryAssemblyName(),
            AppVersion = _appVersion ?? GetEntryAssemblyVersion(),
            Memory = GetMemoryInfo(),
            CorrelationId = _operationContext?.CorrelationId,
            CustomData = customData.Count > 0 ? customData : null
        };
    }

    private static string GetOperatingSystem()
    {
        var os = RuntimeInformation.OSDescription;
        var arch = RuntimeInformation.OSArchitecture.ToString();
        return $"{os} ({arch})";
    }

    private static string? GetEntryAssemblyName()
    {
        try
        {
            return Assembly.GetEntryAssembly()?.GetName().Name;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetEntryAssemblyVersion()
    {
        try
        {
            return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static MemoryInfo GetMemoryInfo()
    {
        var gcInfo = GC.GetGCMemoryInfo();

        return new MemoryInfo
        {
            WorkingSetBytes = Process.GetCurrentProcess().WorkingSet64,
            GcTotalMemoryBytes = GC.GetTotalMemory(forceFullCollection: false),
            GcHeapSizeBytes = gcInfo.HeapSizeBytes,
            GcCollectionCounts =
            [
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2)
            ]
        };
    }
}
