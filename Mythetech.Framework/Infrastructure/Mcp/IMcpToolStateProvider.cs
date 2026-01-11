namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Optional interface for persisting MCP tool enabled/disabled state.
/// If not registered in DI, tool state will be runtime-only (resets on app restart).
/// </summary>
public interface IMcpToolStateProvider
{
    /// <summary>
    /// Load the set of disabled tool names from persistent storage.
    /// </summary>
    /// <returns>Set of tool names that are disabled.</returns>
    Task<IReadOnlySet<string>> LoadDisabledToolsAsync();

    /// <summary>
    /// Save the set of disabled tool names to persistent storage.
    /// </summary>
    /// <param name="disabledTools">Set of tool names that are disabled.</param>
    Task SaveDisabledToolsAsync(IReadOnlySet<string> disabledTools);
}
