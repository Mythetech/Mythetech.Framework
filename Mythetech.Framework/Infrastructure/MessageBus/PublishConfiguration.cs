namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// Configuration for message publishing with timeout and cancellation support.
/// Used to prevent infinite loops and runaway consumers.
/// </summary>
public class PublishConfiguration
{
    /// <summary>
    /// Default timeout of 30 seconds
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Maximum time to wait for all consumers to process the message.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = DefaultTimeout;
    
    /// <summary>
    /// Optional cancellation token for external cancellation.
    /// Will be linked with the timeout-based cancellation.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = default;
    
    /// <summary>
    /// Default configuration with 30 second timeout
    /// </summary>
    public static PublishConfiguration Default => new();
}

