namespace Mythetech.Framework.Observability.Reporting;

/// <summary>
/// Severity level for bug reports.
/// </summary>
public enum BugSeverity
{
    /// <summary>
    /// Low priority issue that doesn't significantly impact functionality.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium priority issue that causes inconvenience but has workarounds.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High priority issue that significantly impacts functionality.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical issue that prevents core functionality from working.
    /// </summary>
    Critical = 3
}
