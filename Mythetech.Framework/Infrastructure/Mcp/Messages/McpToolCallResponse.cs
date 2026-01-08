namespace Mythetech.Framework.Infrastructure.Mcp.Messages;

/// <summary>
/// Response message from tool execution.
/// </summary>
public class McpToolCallResponse
{
    /// <summary>
    /// The tool execution result
    /// </summary>
    public required McpToolResult Result { get; init; }

    /// <summary>
    /// The tool that was executed
    /// </summary>
    public required string ToolName { get; init; }
}
