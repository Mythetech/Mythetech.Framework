using Mythetech.Framework;
using Mythetech.Framework.Components.CommandPalette;

namespace Mythetech.Framework.Storybook.Stories;

/// <summary>
/// Sample command provider for the Storybook command palette demo.
/// </summary>
public sealed class SampleCommandProvider : ICommandProvider
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<PaletteCommand>> GetCommandsAsync(string query, CancellationToken ct)
    {
        IReadOnlyList<PaletteCommand> commands = new PaletteCommand[]
        {
            new("nav.home", "Home", "Navigate to dashboard", MythetechFrameworkIcons.Home,
                new[] { "dashboard", "main" }, _ => Task.CompletedTask, "Navigation"),
            new("nav.settings", "Settings", "Open application settings", MythetechFrameworkIcons.Settings,
                new[] { "preferences", "config" }, _ => Task.CompletedTask, "Navigation"),
            new("nav.users", "Users", "Manage team members", MythetechFrameworkIcons.Round("group"),
                new[] { "team", "members" }, _ => Task.CompletedTask, "Navigation"),
            new("action.search", "Search", "Search the codebase", MythetechFrameworkIcons.Search,
                new[] { "find", "lookup" }, _ => Task.CompletedTask, "Actions"),
            new("action.theme", "Toggle Theme", "Switch between light and dark mode", MythetechFrameworkIcons.DarkMode,
                new[] { "dark", "light", "appearance" }, _ => Task.CompletedTask, "Actions"),
            new("action.help", "Help", "View documentation and support", MythetechFrameworkIcons.Round("help"),
                new[] { "docs", "support", "faq" }, _ => Task.CompletedTask, "Actions"),
        };

        return ValueTask.FromResult(commands);
    }
}
