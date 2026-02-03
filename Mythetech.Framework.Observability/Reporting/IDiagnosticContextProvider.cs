namespace Mythetech.Framework.Observability.Reporting;

/// <summary>
/// Provides custom diagnostic data to be included in reports.
/// Implement this interface to add application-specific context.
/// </summary>
public interface IDiagnosticContextProvider
{
    /// <summary>
    /// Gets the category name for this provider's data.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Collects diagnostic data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary of diagnostic data.</returns>
    Task<IDictionary<string, object>> CollectAsync(CancellationToken ct = default);
}
