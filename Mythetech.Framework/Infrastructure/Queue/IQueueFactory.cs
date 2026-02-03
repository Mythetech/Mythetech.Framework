namespace Mythetech.Framework.Infrastructure.Queue;

/// <summary>
/// Factory for creating named queue instances.
/// Implementations are platform-specific (Desktop uses LiteDB, WebAssembly uses IndexedDB).
/// </summary>
public interface IQueueFactory
{
    /// <summary>
    /// Get or create a queue with the specified name.
    /// Queue instances are cached and reused for the same name.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    /// <param name="queueName">Name for the queue (used as collection/table name).</param>
    /// <returns>A queue instance, or null if storage is unavailable.</returns>
    IQueue<T>? GetQueue<T>(string queueName) where T : class;

    /// <summary>
    /// Get all queue names that have been created.
    /// </summary>
    IEnumerable<string> GetQueueNames();

    /// <summary>
    /// Delete a queue and all its entries.
    /// </summary>
    /// <param name="queueName">Name of the queue to delete.</param>
    /// <returns>True if the queue existed and was deleted.</returns>
    Task<bool> DeleteQueueAsync(string queueName);
}
