namespace Mythetech.Framework.Observability.Outbox;

/// <summary>
/// The type of report in the outbox.
/// </summary>
public enum ReportType
{
    /// <summary>
    /// A crash/exception report.
    /// </summary>
    Crash = 0,

    /// <summary>
    /// A user-initiated bug report.
    /// </summary>
    Bug = 1
}
