namespace Mythetech.Framework.Infrastructure.MessageBus;

/// <summary>
/// Configuration options for query operations via SendAsync.
/// </summary>
public class QueryConfiguration
{
    /// <summary>
    /// The timeout for the query operation. Defaults to 30 seconds.
    /// Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Cancellation token to cancel the query operation.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
