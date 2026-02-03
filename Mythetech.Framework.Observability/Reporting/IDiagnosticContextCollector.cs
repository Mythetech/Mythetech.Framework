namespace Mythetech.Framework.Observability.Reporting;

/// <summary>
/// Collects diagnostic context from all registered providers.
/// </summary>
public interface IDiagnosticContextCollector
{
    /// <summary>
    /// Collects diagnostic context from all registered providers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The collected diagnostic context.</returns>
    Task<DiagnosticContext> CollectAsync(CancellationToken ct = default);
}
