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
    ValueTask<IReadOnlyList<PaletteCommand>> GetCommandsAsync(CancellationToken ct);
}
