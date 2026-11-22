namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// The claude CLI calls that register this app's HTTP MCP server with Claude Code.
/// </summary>
internal static class ClaudeCodeMcpCommands
{
    // claude mcp add refuses a name that already exists, so an entry added before the token is removed first.
    public static IReadOnlyList<IReadOnlyList<string>> Add(string serverName, string endpoint, string? bearerToken)
    {
        List<string> add = ["mcp", "add", "--transport", "http", serverName, endpoint];
        if (bearerToken is not null)
        {
            add.AddRange(["--header", $"Authorization: Bearer {bearerToken}"]);
        }

        return [["mcp", "remove", serverName], add];
    }
}
