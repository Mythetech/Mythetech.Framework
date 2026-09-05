namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// Resolves declared actions against the user's overrides and owns every
/// mutation to those overrides.
/// </summary>
public interface IKeyBindingService
{
    /// <summary>Returns every action with the binding currently in effect.</summary>
    IReadOnlyList<ResolvedKeyBinding> GetAll();

    /// <summary>Returns every action grouped by category, for display.</summary>
    IEnumerable<IGrouping<string, ResolvedKeyBinding>> GetByCategory();

    /// <summary>Returns the binding in effect for an action, or null when unbound or unknown.</summary>
    KeyBinding? GetBinding(string id);

    /// <summary>
    /// Assigns a binding to an action. Assigning the action's default removes
    /// the override instead of storing one. Persists immediately.
    /// </summary>
    Task SetBindingAsync(string id, KeyBinding binding);

    /// <summary>Removes an action's shortcut. Persists immediately.</summary>
    Task UnbindAsync(string id);

    /// <summary>Removes an action's override, returning it to its declared default. Persists immediately.</summary>
    Task ResetAsync(string id);

    /// <summary>Removes every override. Persists immediately.</summary>
    Task ResetAllAsync();

    /// <summary>
    /// Reports whether another action already holds the proposed binding.
    /// </summary>
    /// <param name="id">The action being assigned, excluded from the search.</param>
    /// <param name="proposed">The binding the user wants to assign.</param>
    /// <param name="conflictingId">The id of the action holding it, when one does.</param>
    bool TryFindConflict(string id, KeyBinding proposed, out string? conflictingId);

    /// <summary>
    /// Assigns a binding, unbinding whichever action already held it. Both
    /// changes persist together. Persists immediately.
    /// </summary>
    Task ReassignAsync(string id, KeyBinding binding);

    /// <summary>
    /// Runs an action's handler. An id with no registered definition is logged
    /// and ignored, because a persisted override can outlive a removed action.
    /// A handler that throws is logged and swallowed so a faulty action cannot
    /// surface as an opaque failure at the JS interop boundary.
    /// </summary>
    Task InvokeAsync(string id, CancellationToken cancellationToken);
}
