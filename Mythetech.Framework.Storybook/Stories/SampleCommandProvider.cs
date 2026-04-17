using MudBlazor;
using Mythetech.Framework.Components.CommandPalette;

namespace Mythetech.Framework.Storybook.Stories;

/// <summary>
/// Sample command provider for the Storybook command palette demo.
/// </summary>
public sealed class SampleCommandProvider : ICommandProvider
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<PaletteCommand>> GetCommandsAsync(CancellationToken ct)
    {
        IReadOnlyList<PaletteCommand> commands = new PaletteCommand[]
        {
            new("nav.home", "Home", "Navigate to dashboard", Icons.Material.Filled.Home,
                new[] { "dashboard", "main" }, _ => Task.CompletedTask, "Navigation"),
            new("nav.settings", "Settings", "Open application settings", Icons.Material.Filled.Settings,
                new[] { "preferences", "config" }, _ => Task.CompletedTask, "Navigation"),
            new("nav.users", "Users", "Manage team members", Icons.Material.Filled.People,
                new[] { "team", "members" }, _ => Task.CompletedTask, "Navigation"),
            new("action.search", "Search", "Search the codebase", Icons.Material.Filled.Search,
                new[] { "find", "lookup" }, _ => Task.CompletedTask, "Actions"),
            new("action.theme", "Toggle Theme", "Switch between light and dark mode", Icons.Material.Filled.DarkMode,
                new[] { "dark", "light", "appearance" }, _ => Task.CompletedTask, "Actions"),
            new("action.help", "Help", "View documentation and support", Icons.Material.Filled.Help,
                new[] { "docs", "support", "faq" }, _ => Task.CompletedTask, "Actions"),
        };

        return ValueTask.FromResult(commands);
    }
}
