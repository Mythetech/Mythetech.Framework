using System.Text.Json;

namespace Mythetech.Framework.Infrastructure.Mcp.Messages;

/// <summary>
/// Query message representing an MCP tool call.
/// Routes through IMessageBus.SendAsync for execution.
/// </summary>
public class McpToolCallMessage
{
    /// <summary>
    /// The tool name being invoked
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// The raw JSON arguments from the MCP request
    /// </summary>
    public JsonElement? Arguments { get; init; }

    /// <summary>
    /// Request correlation ID (for telemetry)
    /// </summary>
    public object? RequestId { get; init; }
}
