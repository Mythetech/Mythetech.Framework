using System.Collections.Concurrent;

namespace Mythetech.Framework.Infrastructure.Shell;

/// <summary>
/// Thread-safe implementation of <see cref="ICommandRegistry"/>.
/// </summary>
public class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<string, Func<string[], CancellationToken, Task<ShellResult>>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Register(string name, Func<string[], CancellationToken, Task<ShellResult>> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(handler);

        _handlers[name] = handler;
    }

    /// <inheritdoc />
    public void RegisterSync(string name, Func<string[], ShellResult> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(handler);

        _handlers[name] = (args, _) => Task.FromResult(handler(args));
    }

    /// <inheritdoc />
    public bool TryGetHandler(string name, out Func<string[], CancellationToken, Task<ShellResult>>? handler)
    {
        return _handlers.TryGetValue(name, out handler);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRegisteredCommands() => _handlers.Keys.OrderBy(k => k);

    /// <inheritdoc />
    public bool Unregister(string name) => _handlers.TryRemove(name, out _);
}
