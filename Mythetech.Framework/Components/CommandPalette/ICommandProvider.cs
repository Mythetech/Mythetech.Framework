namespace Mythetech.Framework.Components.CommandPalette;

/// <summary>
/// Source of <see cref="PaletteCommand"/> instances surfaced by the command palette.
/// Called on every palette open so providers can return context-dependent commands.
/// Consuming apps register their own implementations via
/// <c>services.AddCommandProvider&lt;T&gt;()</c>.
/// </summary>
public interface ICommandProvider
{
    /// <summary>Returns the commands this provider supplies to the palette.</summary>
    /// <param name="query">The raw user query. Providers can use this to filter by prefix conventions (e.g. ">" for commands-only).</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask<IReadOnlyList<PaletteCommand>> GetCommandsAsync(string query, CancellationToken ct);
}
