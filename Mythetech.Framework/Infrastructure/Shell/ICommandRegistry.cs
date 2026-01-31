namespace Mythetech.Framework.Infrastructure.Shell;

/// <summary>
/// Registry for commands that can be executed via <see cref="IShellExecutor"/>.
/// Primarily used in WebAssembly where native shell execution isn't available.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// Registers an async command handler.
    /// </summary>
    /// <param name="name">The command name (e.g., "echo", "help").</param>
    /// <param name="handler">Handler that receives arguments and returns a result.</param>
    void Register(string name, Func<string[], CancellationToken, Task<ShellResult>> handler);

    /// <summary>
    /// Registers a synchronous command handler.
    /// The handler will be wrapped to run asynchronously.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="handler">Handler that receives arguments and returns a result.</param>
    void RegisterSync(string name, Func<string[], ShellResult> handler);

    /// <summary>
    /// Attempts to retrieve a command handler by name.
    /// </summary>
    /// <param name="name">The command name to look up.</param>
    /// <param name="handler">The handler if found, null otherwise.</param>
    /// <returns>True if a handler was found.</returns>
    bool TryGetHandler(string name, out Func<string[], CancellationToken, Task<ShellResult>>? handler);

    /// <summary>
    /// Gets all registered command names.
    /// </summary>
    IEnumerable<string> GetRegisteredCommands();

    /// <summary>
    /// Removes a command registration.
    /// </summary>
    /// <param name="name">The command name to unregister.</param>
    /// <returns>True if the command was found and removed.</returns>
    bool Unregister(string name);
}
