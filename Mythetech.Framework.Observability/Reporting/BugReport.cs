namespace Mythetech.Framework.Observability.Reporting;

/// <summary>
/// Represents a user-initiated bug report.
/// </summary>
public record BugReport
{
    /// <summary>
    /// Gets the unique identifier for this report.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets the timestamp when the report was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the user's description of the issue.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the severity level.
    /// </summary>
    public BugSeverity Severity { get; init; } = BugSeverity.Medium;

    /// <summary>
    /// Gets optional steps to reproduce the issue.
    /// </summary>
    public string? StepsToReproduce { get; init; }

    /// <summary>
    /// Gets optional expected behavior.
    /// </summary>
    public string? ExpectedBehavior { get; init; }

    /// <summary>
    /// Gets optional actual behavior.
    /// </summary>
    public string? ActualBehavior { get; init; }

    /// <summary>
    /// Gets the diagnostic context collected at report time.
    /// </summary>
    public DiagnosticContext? Diagnostics { get; init; }

    /// <summary>
    /// Gets optional user contact information.
    /// </summary>
    public string? ContactEmail { get; init; }

    /// <summary>
    /// Gets optional additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Request to create a bug report.
/// </summary>
public record BugReportRequest
{
    /// <summary>
    /// Gets or sets the user's description of the issue.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public BugSeverity Severity { get; init; } = BugSeverity.Medium;

    /// <summary>
    /// Gets or sets optional steps to reproduce the issue.
    /// </summary>
    public string? StepsToReproduce { get; init; }

    /// <summary>
    /// Gets or sets optional expected behavior.
    /// </summary>
    public string? ExpectedBehavior { get; init; }

    /// <summary>
    /// Gets or sets optional actual behavior.
    /// </summary>
    public string? ActualBehavior { get; init; }

    /// <summary>
    /// Gets or sets optional user contact information.
    /// </summary>
    public string? ContactEmail { get; init; }

    /// <summary>
    /// Gets or sets optional additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
